# GenerateEF.ps1 - Script corregido
param(
    [string]$OutputDir = "DataAccess/Generated",
    [string]$ConnectionName = "Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=root;"
)

Write-Host "=== Generando Entity Framework Models ===" -ForegroundColor Green

# Crear directorio si no existe
$fullOutputPath = Join-Path (Get-Location) $OutputDir
if (-not (Test-Path $fullOutputPath)) {
    Write-Host "Creando directorio: $fullOutputPath" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $fullOutputPath -Force | Out-Null
}

Get-ChildItem -Path $fullOutputPath -Include *.* -File -Recurse | foreach { $_.Delete()}

# Ejecutar scaffold
Write-Host "Ejecutando scaffolding..." -ForegroundColor Yellow
try {
    dotnet ef dbcontext scaffold "$ConnectionName" `
        Npgsql.EntityFrameworkCore.PostgreSQL `
        --output-dir Generated `
        --context-dir Generated `
        --context PostgresContext `
        --project DataAccess `
        --startup-project API_Clean_Architecture `
        --force `
		--no-onconfiguring
    
    if ($LASTEXITCODE -ne 0) {
        throw "El comando dotnet ef fallo con codigo: $LASTEXITCODE"
    }
}
catch {
    Write-Host "Error en scaffolding: $_" -ForegroundColor Red
    exit 1
}

# Verificar que se generaron archivos
$generatedFiles = Get-ChildItem -Path $OutputDir -Filter "*.cs" -ErrorAction SilentlyContinue
if ($generatedFiles.Count -eq 0) {
    Write-Host "No se generaron archivos .cs en $OutputDir" -ForegroundColor Red
    exit 1
}

Write-Host "Se generaron $($generatedFiles.Count) archivos" -ForegroundColor Green

# Renombrar archivos y clases (excluyendo el DbContext)
$renames = @{}
$contextFile = $null

foreach ($file in $generatedFiles) {
    $baseName = $file.BaseName
    
    # Saltar el DbContext
    if ($baseName -eq "PostgresContext") {
        $contextFile = $file
        Write-Host "Saltando DbContext: $baseName" -ForegroundColor Cyan
        continue
    }
    
    $newName = $baseName + "DB"
    $newFileName = $newName + ".cs"
    $newPath = Join-Path $file.Directory.FullName $newFileName
    
    $renames[$baseName] = $newName
    
    try {
        # Renombrar archivo
        Rename-Item -Path $file.FullName -NewName $newFileName -Force
        
        # Actualizar contenido del archivo
        $content = Get-Content -Path $newPath -Encoding UTF8
        $updatedContent = @()
        
        foreach ($line in $content) {
            # Actualizar declaracion de clase
            if ($line -match "public partial class $baseName\b") {
                $line = $line -replace "public partial class $baseName\b", "public partial class $newName"
            }
            # Actualizar referencias en navegacion
            $updatedContent += $line
            $line = $line -replace "\b$baseName\b", $newName
        }
		
        $updatedContent | Set-Content -Path $newPath -Encoding UTF8
        Write-Host "$baseName -> $newName" -ForegroundColor Green
    }
    catch {
        Write-Host "Error procesando $baseName : $_" -ForegroundColor Red
    }
}

# Actualizar DbContext si existe
if ($contextFile -and $renames.Count -gt 0) {
    Write-Host "Actualizando DbContext..." -ForegroundColor Yellow
    
    try {
        $contextContent = Get-Content -Path $contextFile.FullName -Encoding UTF8
        $updatedContextContent = @()
        
        foreach ($line in $contextContent) {
            $updatedLine = $line
            
            # Actualizar referencias en el DbContext
            foreach ($oldName in $renames.Keys) {
                $newName = $renames[$oldName]
                
                # Actualizar DbSet<T>
                $updatedLine = $updatedLine -replace "DbSet<$oldName>", "DbSet<$newName>"
                
                # Actualizar Entity<T> en OnModelCreating
                $updatedLine = $updatedLine -replace "Entity<$oldName>", "Entity<$newName>"
                
                # Actualizar referencias en configuraciones
                $updatedLine = $updatedLine -replace "\b$oldName\b", $newName
            }
            
            $updatedContextContent += $updatedLine
        }
        
        $updatedContextContent | Set-Content -Path $contextFile.FullName -Encoding UTF8
        Write-Host "DbContext actualizado" -ForegroundColor Green
    }
    catch {
        Write-Host "Error actualizando DbContext: $_" -ForegroundColor Red
    }
}

$renamedFiles = Get-ChildItem -Path $OutputDir -Filter "*.cs" -ErrorAction SilentlyContinue

foreach ($file in $renamedFiles) {
    $content = Get-Content -Path $file.FullName -Encoding UTF8
    $updatedContent = @()

    foreach ($line in $content) {
        $updatedLine = $line

        foreach ($old in $renames.Keys) {
            $new = $renames[$old]

            # Reemplazar propiedades simples
            $updatedLine = $updatedLine -replace "public virtual $old\b", "public virtual $new"

            # Reemplazar ICollection<T>
            $updatedLine = $updatedLine -replace "ICollection<$old>", "ICollection<$new>"

            # Reemplazar List<T> (por si acaso)
            $updatedLine = $updatedLine -replace "List<$old>", "List<$new>"
        }

        $updatedContent += $updatedLine
    }

    $updatedContent | Set-Content -Path $file.FullName -Encoding UTF8
}

# Resumen
Write-Host ""
Write-Host "=== Resumen ===" -ForegroundColor Green
Write-Host "Directorio: $OutputDir"
Write-Host "Modelos renombrados: $($renames.Count)"
Write-Host "DbContext: PostgresContext.cs"

if ($renames.Count -gt 0) {
    Write-Host ""
    Write-Host "Cambios realizados:" -ForegroundColor Cyan
    foreach ($old in $renames.Keys) {
        Write-Host "   $old -> $($renames[$old])"
    }
}

Write-Host ""
Write-Host "Proceso completado exitosamente!" -ForegroundColor Green