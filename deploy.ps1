# ============================================================
# Akyildiz Sevkiyat — Deploy Script (release + symlink deseni)
# Kullanim: .\deploy.ps1
#
# ON KOSUL (sunucuda bir kere yapilacak):
#   1) Klasorler:
#        sudo mkdir -p /var/www/releases/api /var/www/releases/web
#        sudo chown -R ensaradmin:ensaradmin /var/www/releases
#   2) Ilk symlink'ler (mevcut canli surumu ilk release olarak tasi):
#        mv /var/www/sevkiyat-api /var/www/releases/api/initial
#        ln -s /var/www/releases/api/initial /var/www/sevkiyat-api
#        (ayni islemi sevkiyat-app icin de yap)
#      NOT: systemd unit'i ve nginx root'u symlink yolunu gosterdigi
#      icin hicbir config degisikligi gerekmez.
#   3) sudoers (sudo visudo ile, sifresiz systemctl icin):
#        ensaradmin ALL=(ALL) NOPASSWD: /usr/bin/systemctl stop sevkiyat-api, /usr/bin/systemctl start sevkiyat-api, /usr/bin/systemctl restart sevkiyat-api, /usr/bin/systemctl status sevkiyat-api
# ============================================================

$SERVER        = "ensaradmin@204.168.205.59"
$RELEASES_API  = "/var/www/releases/api"
$RELEASES_WEB  = "/var/www/releases/web"
$LINK_API      = "/var/www/sevkiyat-api"
$LINK_WEB      = "/var/www/sevkiyat-app"
$SERVICE_NAME  = "sevkiyat-api"
$HEALTH_URL    = "http://localhost:5000/health"   # kendi health endpoint'ine gore duzelt
$KEEP_RELEASES = 5
$PUBLISH_DIR   = ".\publish"
$CLIENT_DIR    = ".\client"

$STAMP = Get-Date -Format "yyyyMMdd-HHmmss"

function Fail($msg) { Write-Error $msg; exit 1 }

# --- [1/6] .NET publish -------------------------------------
Write-Host "`n=== [1/6] .NET publish ===" -ForegroundColor Cyan
if (Test-Path $PUBLISH_DIR) { Remove-Item $PUBLISH_DIR -Recurse -Force }
dotnet publish Akyildiz.Sevkiyat.WebApi --configuration Release --output $PUBLISH_DIR --no-self-contained
if ($LASTEXITCODE -ne 0) { Fail "dotnet publish basarisiz." }

# --- [2/6] Vue build ----------------------------------------
Write-Host "`n=== [2/6] Vue frontend build ===" -ForegroundColor Cyan
Push-Location $CLIENT_DIR
if (Test-Path "dist") { Remove-Item "dist" -Recurse -Force -ErrorAction SilentlyContinue }
npm run build
$buildExit = $LASTEXITCODE
Pop-Location
if ($buildExit -ne 0) { Fail "npm run build basarisiz." }

# --- [3/6] Yeni release klasorlerine yukle -------------------
# Canli surume DOKUNMADAN yan klasore yukluyoruz. Servis calismaya devam ediyor.
Write-Host "`n=== [3/6] Release yukleniyor: $STAMP ===" -ForegroundColor Cyan
ssh $SERVER "mkdir -p ${RELEASES_API}/${STAMP} ${RELEASES_WEB}/${STAMP}"
if ($LASTEXITCODE -ne 0) { Fail "Sunucuda release klasoru olusturulamadi." }

scp -r "${PUBLISH_DIR}/." "${SERVER}:${RELEASES_API}/${STAMP}/"
if ($LASTEXITCODE -ne 0) { Fail "API dosyalari yuklenemedi. Canli surum etkilenmedi." }

scp -r "${CLIENT_DIR}/dist/." "${SERVER}:${RELEASES_WEB}/${STAMP}/"
if ($LASTEXITCODE -ne 0) { Fail "Frontend dosyalari yuklenemedi. Canli surum etkilenmedi." }

# --- [4/6] Atomik gecis (symlink swap) -----------------------
Write-Host "`n=== [4/6] Symlink swap + servis restart ===" -ForegroundColor Cyan
# ln -sfn: symlink'i tek hamlede yeni release'e cevirir
ssh $SERVER "ln -sfn ${RELEASES_API}/${STAMP} ${LINK_API} && ln -sfn ${RELEASES_WEB}/${STAMP} ${LINK_WEB} && sudo systemctl restart ${SERVICE_NAME}"
if ($LASTEXITCODE -ne 0) { Fail "Symlink/restart basarisiz. Rollback icin: .\rollback.ps1" }

# --- [5/6] Health check --------------------------------------
Write-Host "`n=== [5/6] Health check ===" -ForegroundColor Cyan
Start-Sleep -Seconds 5
ssh $SERVER "curl -sf --max-time 10 ${HEALTH_URL} > /dev/null"
if ($LASTEXITCODE -ne 0) {
    Write-Host "HEALTH CHECK BASARISIZ! Uygulama ayakta degil." -ForegroundColor Red
    Write-Host "Geri donmek icin: .\rollback.ps1" -ForegroundColor Yellow
    exit 1
}
Write-Host "Health check OK." -ForegroundColor Green

# --- [6/6] Eski release temizligi ----------------------------
Write-Host "`n=== [6/6] Eski release'ler temizleniyor (son $KEEP_RELEASES kalir) ===" -ForegroundColor Cyan
ssh $SERVER "ls -1dt ${RELEASES_API}/*/ | tail -n +$($KEEP_RELEASES + 1) | xargs -r rm -rf"
ssh $SERVER "ls -1dt ${RELEASES_WEB}/*/ | tail -n +$($KEEP_RELEASES + 1) | xargs -r rm -rf"

Write-Host "`nDeploy tamamlandi: $STAMP" -ForegroundColor Green
