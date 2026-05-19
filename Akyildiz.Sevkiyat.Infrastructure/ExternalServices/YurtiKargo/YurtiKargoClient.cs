using System.Text;
using System.Xml.Linq;
using Akyildiz.Sevkiyat.Application.External.YurtiKargo;
using Akyildiz.Sevkiyat.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Akyildiz.Sevkiyat.Infrastructure.ExternalServices.YurtiKargo
{
    public class YurtiKargoClient : IYurtiKargoClient
    {
        private static readonly XNamespace SoapEnv = "http://schemas.xmlsoap.org/soap/envelope/";
        private static readonly XNamespace WsNs    = "http://yurticikargo.com.tr/ShippingOrderDispatcherServices";

        private readonly HttpClient _http;
        private readonly YurtiKargoOptions _opts;
        private readonly ILogger<YurtiKargoClient> _log;

        public YurtiKargoClient(HttpClient http, IOptions<YurtiKargoOptions> opts, ILogger<YurtiKargoClient> log)
        {
            _http = http;
            _opts = opts.Value;
            _log  = log;
        }

        public async Task<YkCreateShipmentResult> CreateShipmentAsync(YkCreateShipmentRequest req, CancellationToken ct = default)
        {
            if (!_opts.IsConfigured)
                return Fail("Yurtici Kargo entegrasyonu yapılandırılmamış (WsUserName/WsPassword eksik).");

            var envelope = BuildEnvelope("createShipment", new XElement("ShippingOrderVO",
                new XElement("cargoKey",          req.CargoKey),
                new XElement("invoiceKey",         req.InvoiceKey),
                new XElement("receiverCustName",   req.ReceiverName),
                new XElement("receiverAddress",    req.ReceiverAddress),
                new XElement("receiverPhone1",     req.ReceiverPhone),
                req.ReceiverPhone2   != null ? new XElement("receiverPhone2", req.ReceiverPhone2) : null!,
                req.ReceiverCityName != null ? new XElement("cityName",       req.ReceiverCityName) : null!,
                req.ReceiverTownName != null ? new XElement("townName",       req.ReceiverTownName) : null!,
                new XElement("desi",       req.Desi.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XElement("kg",         req.Kg.ToString("F2",   System.Globalization.CultureInfo.InvariantCulture)),
                new XElement("cargoCount", req.PieceCount),
                new XElement("taxOfficeId",      0),
                new XElement("ttDocumentId",     0),
                new XElement("dcSelectedCredit", 0),
                new XElement("dcCreditRule",     0)
            ));

            string responseBody;
            try
            {
                responseBody = await PostSoapRawAsync("createShipment", envelope, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[YK] createShipment HTTP hatası. CargoKey={CargoKey}", req.CargoKey);
                return Fail(ex.Message);
            }

            XDocument response;
            try
            {
                response = XDocument.Parse(responseBody);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[YK] createShipment yanıtı parse edilemedi. CargoKey={CargoKey} Body={Body}",
                    req.CargoKey, responseBody);
                return Fail("Geçersiz XML yanıtı.");
            }

            // Yurtiçi KOPS V3: ShippingOrderResultVO
            var detail = response.Descendants("ShippingOrderResultVO").FirstOrDefault()
                      ?? response.Descendants("shippingOrderDetailVO").FirstOrDefault()
                      ?? response.Descendants("return").FirstOrDefault();

            if (detail == null)
            {
                _log.LogWarning("[YK] createShipment yanıtında beklenen element bulunamadı. CargoKey={CargoKey} Body={Body}",
                    req.CargoKey, responseBody);
                return Fail("Geçersiz yanıt formatı.");
            }

            // outFlag: 0=başarılı, 1=hata  |  outResult: mesaj  |  errCode: hata kodu
            var outFlagStr       = detail.Element("outFlag")?.Value;
            var operationMessage = detail.Element("outResult")?.Value;
            var errCode          = detail.Element("errCode")?.Value;
            var docId            = detail.Element("docId")?.Value;
            var invoiceKey       = detail.Element("invoiceKey")?.Value;
            var barcode          = detail.Element("barcode")?.Value;
            int? jobId           = int.TryParse(detail.Element("jobId")?.Value, out var j) && j != 0 ? j : null;

            var success = outFlagStr == "0";

            if (!success)
            {
                _log.LogWarning(
                    "[YK] createShipment başarısız. CargoKey={CargoKey} outFlag={outFlag} ErrCode={ErrCode} OutResult={OutResult}",
                    req.CargoKey, outFlagStr, errCode, operationMessage);

                return new YkCreateShipmentResult(
                    IsSuccess: false,
                    ErrorMessage: operationMessage ?? $"Hata kodu: {errCode}",
                    OperationStatus: outFlagStr,
                    OperationMessage: operationMessage,
                    ErrCode: errCode,
                    ErrMessage: operationMessage,
                    JobId: null,
                    DocId: docId,
                    InvoiceKey: invoiceKey,
                    Barcode: null
                );
            }

            _log.LogInformation(
                "[YK] createShipment başarılı. CargoKey={CargoKey} JobId={JobId} DocId={DocId} Barcode={Barcode}",
                req.CargoKey, jobId, docId, barcode ?? "(boş)");

            return new YkCreateShipmentResult(
                IsSuccess: true,
                ErrorMessage: null,
                OperationStatus: outFlagStr,
                OperationMessage: operationMessage,
                ErrCode: null,
                ErrMessage: null,
                JobId: jobId,
                DocId: docId,
                InvoiceKey: invoiceKey,
                Barcode: barcode
            );
        }

        public async Task<bool> CancelShipmentAsync(string cargoKey, CancellationToken ct = default)
        {
            if (!_opts.IsConfigured) return false;

            // cancelShipment uses userLanguage (same as createShipment)
            var envelope = BuildEnvelope("cancelShipment",
                new XElement("cargoKeys", cargoKey));

            string responseBody;
            try
            {
                responseBody = await PostSoapRawAsync("cancelShipment", envelope, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[YK] cancelShipment HTTP hatası. CargoKey={CargoKey}", cargoKey);
                return false;
            }

            var response = XDocument.Parse(responseBody);
            var outFlag  = response.Descendants("outFlag").FirstOrDefault()?.Value;

            var ok = outFlag == "0";
            if (!ok)
                _log.LogWarning("[YK] cancelShipment başarısız. CargoKey={CargoKey} outFlag={outFlag} Body={Body}",
                    cargoKey, outFlag, responseBody);

            return ok;
        }

        public async Task<YkShipmentStatus?> QueryShipmentAsync(string cargoKey, CancellationToken ct = default)
        {
            if (!_opts.IsConfigured) return null;

            // queryShipment uses wsLanguage (different from createShipment/cancelShipment)
            var envelope = BuildEnvelope("queryShipment", languageField: "wsLanguage",
                new XElement("keys",               cargoKey),
                new XElement("keyType",            0),
                new XElement("addHistoricalData",  false),
                new XElement("onlyTracking",       false));

            string responseBody;
            try
            {
                responseBody = await PostSoapRawAsync("queryShipment", envelope, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "[YK] queryShipment HTTP hatası. CargoKey={CargoKey}", cargoKey);
                return null;
            }

            _log.LogDebug("[YK] queryShipment yanıt. CargoKey={CargoKey} Body={Body}", cargoKey, responseBody);

            var response = XDocument.Parse(responseBody);
            var detail   = response.Descendants("shippingDeliveryDetailVO").FirstOrDefault();

            if (detail == null) return null;

            var operationStatus  = detail.Element("operationStatus")?.Value;
            var operationMessage = detail.Element("operationMessage")?.Value;
            var barcode          = detail.Element("barcode")?.Value;

            _log.LogInformation(
                "[YK] queryShipment yanıt. CargoKey={CargoKey} Status={Status} Barcode={Barcode}",
                cargoKey, operationStatus ?? "(boş)", barcode ?? "(boş)");

            return new YkShipmentStatus(cargoKey, operationStatus, operationMessage, barcode, null);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static YkCreateShipmentResult Fail(string message) =>
            new(false, message, null, null, null, null, null, null, null, null);

        private XDocument BuildEnvelope(string operation, params XElement[] bodyChildren)
            => BuildEnvelope(operation, "userLanguage", bodyChildren);

        private XDocument BuildEnvelope(string operation, string languageField, params XElement[] bodyChildren)
        {
            var body = new XElement(WsNs + operation,
                new XElement("wsUserName",  _opts.WsUserName),
                new XElement("wsPassword",  _opts.WsPassword),
                new XElement(languageField, "TR")
            );

            foreach (var child in bodyChildren)
                if (child != null) body.Add(child);

            return new XDocument(
                new XElement(SoapEnv + "Envelope",
                    new XAttribute(XNamespace.Xmlns + "soapenv", SoapEnv),
                    new XAttribute(XNamespace.Xmlns + "ws",      WsNs),
                    new XElement(SoapEnv + "Header"),
                    new XElement(SoapEnv + "Body", body)
                )
            );
        }

        private async Task<string> PostSoapRawAsync(string action, XDocument envelope, CancellationToken ct)
        {
            var xml      = envelope.ToString(SaveOptions.DisableFormatting);
            var content  = new StringContent(xml, Encoding.UTF8, "text/xml");
            content.Headers.Add("SOAPAction", $"\"{WsNs}{action}\"");

            _log.LogDebug("[YK] SOAP isteği gönderiliyor. Action={Action} Payload={Payload}", action, xml);

            var response = await _http.PostAsync("", content, ct);
            var body     = await response.Content.ReadAsStringAsync(ct);

            _log.LogDebug("[YK] SOAP yanıtı alındı. Action={Action} StatusCode={StatusCode} Body={Body}",
                action, (int)response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Yurtici Kargo SOAP HTTP {(int)response.StatusCode}. Yanıt: {body}");

            return body;
        }
    }
}
