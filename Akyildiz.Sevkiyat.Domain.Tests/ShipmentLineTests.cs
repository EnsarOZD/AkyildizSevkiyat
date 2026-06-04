using Akyildiz.Sevkiyat.Domain.Entities;
using Akyildiz.Sevkiyat.Domain.Enums;

namespace Akyildiz.Sevkiyat.Domain.Tests;

/// <summary>
/// ShipmentLine.NetDeliveredQty — teslimde stoktan düşülecek net miktar.
/// İade edilen kalem teslim/çıkış sayılmaz (Faz 2 / #10).
/// </summary>
public class ShipmentLineTests
{
    private static ShipmentLine Make(decimal orderedQty)
        => ShipmentLine.Create(null, 1, "STK-001", "Test Stok", StockUnit.Adet, orderedQty);

    // ── NetDeliveredQty ───────────────────────────────────────────────────────

    [Fact]
    public void NetDeliveredQty_NoPickingNoReturn_UsesOrderedQty()
    {
        var line = Make(orderedQty: 10);
        Assert.Equal(10, line.NetDeliveredQty);
    }

    [Fact]
    public void NetDeliveredQty_PickedLessNoReturn_UsesDeliveredQty()
    {
        var line = Make(orderedQty: 10);
        line.SetDeliveredQty(8, reason: "eksik toplama");
        Assert.Equal(8, line.NetDeliveredQty);
    }

    [Fact]
    public void NetDeliveredQty_PartialReturnNoPicking_SubtractsFromOrdered()
    {
        var line = Make(orderedQty: 10);
        line.RecordReturn(3, null);
        Assert.Equal(7, line.NetDeliveredQty);
    }

    [Fact]
    public void NetDeliveredQty_PartialReturnWithPicking_SubtractsFromDelivered()
    {
        var line = Make(orderedQty: 10);
        line.SetDeliveredQty(8, reason: "eksik toplama");
        line.RecordReturn(3, null);
        Assert.Equal(5, line.NetDeliveredQty);
    }

    [Fact]
    public void NetDeliveredQty_FullReturn_IsZero()
    {
        var line = Make(orderedQty: 10);
        line.RecordReturn(10, null);
        Assert.Equal(0, line.NetDeliveredQty);
    }

    [Fact]
    public void NetDeliveredQty_ReturnExceedsLoaded_ClampsToZero()
    {
        var line = Make(orderedQty: 10);
        line.RecordReturn(15, null); // savunma amaçlı — normalde olmamalı
        Assert.Equal(0, line.NetDeliveredQty);
    }

    [Fact]
    public void NetDeliveredQty_ZeroPickedWithReturn_FallsBackToOrdered()
    {
        var line = Make(orderedQty: 10);
        line.SetDeliveredQty(0, reason: "hiç toplanmadı");
        line.RecordReturn(4, null);
        // DeliveredQty 0 → loaded = OrderedQty (10); net = 10 - 4 = 6
        Assert.Equal(6, line.NetDeliveredQty);
    }

    // ── Stok tutarlılığı: iki sıralama aynı net sonuca yakınsamalı (#10) ────────
    // Senaryo A: önce teslim (tam çıkış), sonra iade (geri ekleme)
    // Senaryo B: önce iade (çıkış yok), sonra teslim (net çıkış)
    // Her ikisi de aynı final OnHandQty vermeli.

    [Fact]
    public void StockConsistency_DeliverThenReturn_EqualsReturnThenDeliver()
    {
        const decimal startOnHand = 100;
        const decimal loaded = 10;
        const decimal returned = 3;

        // Senaryo A — önce teslim, sonra iade
        var stockA = new StockMaster { StockCode = "STK-A", StockName = "A" };
        stockA.Increase(startOnHand);
        var lineA = Make(orderedQty: loaded);
        // teslim anında iade yok → tam yüklenen düşülür
        stockA.Deduct(lineA.NetDeliveredQty);           // 100 - 10 = 90
        // teslim sonrası iade → stoğa geri eklenir
        stockA.Increase(returned);                       // 90 + 3 = 93

        // Senaryo B — önce iade, sonra teslim
        var stockB = new StockMaster { StockCode = "STK-B", StockName = "B" };
        stockB.Increase(startOnHand);
        var lineB = Make(orderedQty: loaded);
        lineB.RecordReturn(returned, null);              // teslimden önce iade kaydı
        stockB.Deduct(lineB.NetDeliveredQty);            // 100 - (10 - 3) = 93

        Assert.Equal(93, stockA.OnHandQty);
        Assert.Equal(93, stockB.OnHandQty);
        Assert.Equal(stockA.OnHandQty, stockB.OnHandQty);
    }

    [Fact]
    public void StockConsistency_FullReturnBeforeDelivery_NoStockOut()
    {
        var stock = new StockMaster { StockCode = "STK-C", StockName = "C" };
        stock.Increase(100);
        var line = Make(orderedQty: 10);
        line.RecordReturn(10, null); // tamamı iade

        var net = line.NetDeliveredQty; // 0
        if (net > 0) stock.Deduct(net); // handler'daki "if (actualQty <= 0) continue" davranışı

        Assert.Equal(0, net);
        Assert.Equal(100, stock.OnHandQty); // stok değişmez
    }
}
