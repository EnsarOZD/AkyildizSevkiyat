# ============================================================
# Akyildiz Sevkiyat — Deploy Script
# Kullanim: .\deploy.ps1
# ============================================================

# --- AYARLAR (sunucuya gore duzenle) ------------------------
$SERVER        = "ensaradmin@204.168.205.59"
$API_PATH      = "/var/www/sevkiyat-api"    # dotnet'in calistigi klasor
$WEB_PATH      = "/var/www/sevkiyat-app"    # nginx'in serve ettigi frontend klasoru
$SERVICE_NAME  = "sevkiyat-api"             # systemd servis adi
$PUBLISH_DIR   = ".\publish"
$CLIENT_DIR    = ".\client"
# ------------------------------------------------------------

Write-Host "`n=== [1/4] .NET publish ===" -ForegroundColor Cyan
if (Test-Path $PUBLISH_DIR) { Remove-Item $PUBLISH_DIR -Recurse -Force }
dotnet publish Akyildiz.Sevkiyat.WebApi `
    --configuration Release `
    --output $PUBLISH_DIR `
    --no-self-contained
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet publish basarisiz."; exit 1 }

Write-Host "`n=== [2/4] Vue frontend build ===" -ForegroundColor Cyan
Push-Location $CLIENT_DIR
if (Test-Path "dist") { Remove-Item "dist" -Recurse -Force -ErrorAction SilentlyContinue }
npm run build
if ($LASTEXITCODE -ne 0) { Pop-Location; Write-Error "npm run build basarisiz."; exit 1 }
Pop-Location

Write-Host "`n=== [3/4] Sunucuya kopyalama ===" -ForegroundColor Cyan

$SUDO_PASS = Read-Host -Prompt "Sudo sifresi" -AsSecureString
$SUDO = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SUDO_PASS))

# API dosyalarini gonder (servis durdurularak)
ssh $SERVER "echo '$SUDO' | sudo -S systemctl stop $SERVICE_NAME"
ssh $SERVER "echo '$SUDO' | sudo -S rm -rf ${API_PATH}/*"
scp -r "${PUBLISH_DIR}/." "${SERVER}:${API_PATH}/"

# Frontend dist'i gonder
ssh $SERVER "echo '$SUDO' | sudo -S rm -rf ${WEB_PATH}/*"
scp -r "client/dist/." "${SERVER}:${WEB_PATH}/"

Write-Host "`n=== [4/4] Servisi baslat ===" -ForegroundColor Cyan
ssh $SERVER "echo '$SUDO' | sudo -S systemctl start $SERVICE_NAME && sleep 3 && echo '$SUDO' | sudo -S systemctl status $SERVICE_NAME --no-pager -l"

Write-Host "`nDeploy tamamlandi!" -ForegroundColor Green
