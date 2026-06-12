# ============================================================
# Akyildiz Sevkiyat — Rollback Script (v2: PowerShell-side filtre)
# Kullanim: .\rollback.ps1
# Bir onceki release'e doner (symlink swap + restart). ~5 saniye.
# ============================================================

$SERVER        = "ensaradmin@204.168.205.59"
$RELEASES_API  = "/var/www/releases/api"
$RELEASES_WEB  = "/var/www/releases/web"
$LINK_API      = "/var/www/sevkiyat-api"
$LINK_WEB      = "/var/www/sevkiyat-app"
$SERVICE_NAME  = "sevkiyat-api"
$HEALTH_URL    = "http://localhost:5000/health"

# Su anki release
$current = (ssh $SERVER "readlink -f $LINK_API").Trim()
Write-Host "Su anki release : $current"

# Tum release'ler (yeniden eskiye), filtre PowerShell tarafinda
$releases = ssh $SERVER "ls -1dt $RELEASES_API/*/" |
    ForEach-Object { $_.Trim().TrimEnd('/') } |
    Where-Object { $_ -and ($_ -ne $current) }

$previous = $releases | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($previous)) {
    Write-Error "Geri donulecek onceki release bulunamadi."
    exit 1
}
$prevStamp = Split-Path $previous -Leaf
Write-Host "Donulecek release: $previous" -ForegroundColor Yellow

$confirm = Read-Host "Devam? (e/h)"
if ($confirm -ne "e") { Write-Host "Iptal edildi."; exit 0 }

ssh $SERVER "ln -sfn $RELEASES_API/$prevStamp $LINK_API && ln -sfn $RELEASES_WEB/$prevStamp $LINK_WEB && sudo systemctl restart $SERVICE_NAME"
if ($LASTEXITCODE -ne 0) { Write-Error "Rollback sirasinda hata!"; exit 1 }

Start-Sleep -Seconds 5
ssh $SERVER "curl -sf --max-time 10 $HEALTH_URL > /dev/null"
if ($LASTEXITCODE -ne 0) {
    Write-Host "DIKKAT: Health check basarisiz (eski surumde /health yoksa beklenen durum)." -ForegroundColor Red
    exit 1
}

Write-Host "`nRollback tamamlandi: $prevStamp" -ForegroundColor Green
