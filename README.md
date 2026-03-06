# 📦 DSA — Sistema de Digitalización Archivística

> Módulo de captura, procesamiento y sellado criptográfico de documentos físicos.
> De papel a archivo digital blindado, en un solo flujo automatizado.

---

## 🗺️ Vista General del Sistema

```mermaid
graph TB
    subgraph FISICO["🏛️ MUNDO FÍSICO"]
        PAPEL[📄 Documento en papel]
        SCANNER[🖨️ Escáner HP / Kodak]
    end

    subgraph PIPELINE["⚙️ PIPELINE DE PROCESAMIENTO"]
        direction TB
        C1["① CAPTURA\nWIA 2.0 · ADF Loop\nSin tocar disco C:"]
        C2["② LIMPIEZA\nSkiaSharp\nDetecta páginas blancas\nCorrige inclinación"]
        C3["③ CONVERSIÓN\niText7\nPDF/A-1b · ISO 19005-1\nPerfil ICC sRGB embebido"]
        C4["④ SELLO SHA-256\nHuella digital única\n64 caracteres hexadecimales"]
        C5["⑤ ALMACENAMIENTO\nEscritura atómica\n.tmp → rename\nRuta UNC institucional"]
        C6["⑥ ACTIVACIÓN SEAL\ndocumento.SetHash\nEstadoVector bit 4 = 1"]
    end

    subgraph RESULTADO["✅ RESULTADO"]
        PDF["📋 PDF/A Archivado\nInmutable · Sellado · Trazable"]
    end

    PAPEL --> SCANNER --> C1 --> C2 --> C3 --> C4 --> C5 --> C6 --> PDF

    style FISICO fill:#f0f4ff,stroke:#4a6fa5,color:#1a1a2e
    style PIPELINE fill:#f0fff4,stroke:#2d6a4f,color:#1a1a2e
    style RESULTADO fill:#fff8e1,stroke:#e6a817,color:#1a1a2e
    style C4 fill:#ffe0e0,stroke:#c0392b,color:#1a1a2e
    style C6 fill:#ffe0e0,stroke:#c0392b,color:#1a1a2e
```

---

## 🏗️ Arquitectura por Capas

```mermaid
graph LR
    subgraph PRES["🖥️ Presentación\nWinUI 3"]
        VM["CapturaViewModel\n+ CancellationToken\n+ IProgress"]
    end

    subgraph APP["📐 Aplicación"]
        DS["DigitizationService\nOrquestador"]
        IS["IScannerService\nContrato"]
    end

    subgraph INFRA["🔧 Infraestructura"]
        SS["ScannerService\nWIA 2.0 · COM Interop"]
        ST["UncStorageService\nAlmacenamiento UNC"]
        DB["DocumentoRepository\nPostgreSQL · EF Core"]
    end

    subgraph DOM["🎯 Dominio"]
        DOC["Documento\nEstadoVector D[11:0]"]
        REPO["IDocumentoRepository"]
        STOR["IStorageService"]
    end

    VM --> DS
    DS --> IS
    DS --> REPO
    DS --> STOR
    IS -.implementa.-> SS
    REPO -.implementa.-> DB
    STOR -.implementa.-> ST
    DS --> DOC

    style PRES fill:#e8f4f8,stroke:#2980b9,color:#1a1a2e
    style APP fill:#eafaf1,stroke:#27ae60,color:#1a1a2e
    style INFRA fill:#fef9e7,stroke:#f39c12,color:#1a1a2e
    style DOM fill:#fdf2f8,stroke:#8e44ad,color:#1a1a2e
```

---

## ⚡ Máquina de Estados del Documento

```mermaid
stateDiagram-v2
    [*] --> INGRESADO : Documento creado\nD[7] = 1

    INGRESADO --> EN_PROCESO : Asignado a mesa\nD[8] = 1

    EN_PROCESO --> EN_VALIDACION : Revisor activo\nD[9] = 1

    EN_VALIDACION --> CLASIFICADO : Taxonomía CADIDO\nD[3] = 1

    CLASIFICADO --> SELLADO : SetHash exitoso\n⚠️ D[4] = 1 · SEAL

    SELLADO --> ARCHIVADO : Transición final\nD[11] = 1

    EN_PROCESO --> RECHAZADO : Documento inválido\nD[10] = 1

    EN_VALIDACION --> RECHAZADO : No cumple criterios\nD[10] = 1

    ARCHIVADO --> [*]
    RECHAZADO --> [*]

    note right of SELLADO
        Sin este bit activo
        el documento NO puede
        transitar a ARCHIVADO
    end note
```

---

## 🔐 El Bit SEAL — Regla de Negocio Central

```mermaid
flowchart TD
    INICIO(["🚀 Inicio del Pipeline"])

    P1{"¿Escáner\ndisponible?"}
    P2{"¿Imágenes\ncapturadas?"}
    P3{"¿PDF/A\ngenerado?"}
    P4{"¿Hash\ncalculado?"}
    P5{"¿Archivo\nguardado?"}
    P6{"¿SetHash\nexitoso?"}

    SEAL(["✅ BIT SEAL ACTIVO\nEstadoVector |= 1 << 4\nDocumento puede archivarse"])

    ERR1(["❌ ABORT\nSin rollback necesario"])
    ERR2(["❌ ABORT\nSin rollback necesario"])
    ERR3(["❌ ABORT\nSin rollback necesario"])
    ERR4(["❌ ABORT\nSin rollback necesario"])
    ERR5(["❌ ABORT + ROLLBACK\nArchivo .pdf eliminado del NAS"])
    ERR6(["❌ ABORT + ROLLBACK\nArchivo .pdf eliminado del NAS\nBit SEAL permanece en 0"])

    INICIO --> P1
    P1 -->|Sí| P2
    P1 -->|No| ERR1
    P2 -->|Sí| P3
    P2 -->|No| ERR2
    P3 -->|Sí| P4
    P3 -->|No| ERR3
    P4 -->|Sí| P5
    P4 -->|No| ERR4
    P5 -->|Sí| P6
    P5 -->|No| ERR5
    P6 -->|Sí| SEAL
    P6 -->|No| ERR6

    style SEAL fill:#d4edda,stroke:#28a745,color:#155724
    style ERR1 fill:#f8d7da,stroke:#dc3545,color:#721c24
    style ERR2 fill:#f8d7da,stroke:#dc3545,color:#721c24
    style ERR3 fill:#f8d7da,stroke:#dc3545,color:#721c24
    style ERR4 fill:#f8d7da,stroke:#dc3545,color:#721c24
    style ERR5 fill:#f8d7da,stroke:#dc3545,color:#721c24
    style ERR6 fill:#f8d7da,stroke:#dc3545,color:#721c24
```

---

## 🧵 Modelo de Hilos (WinUI 3)

```mermaid
sequenceDiagram
    actor Usuario
    participant UI as 🖥️ UI Thread
    participant VM as CapturaViewModel
    participant DS as DigitizationService
    participant HW as ScannerService (COM)
    participant NAS as Almacenamiento UNC

    Usuario->>UI: Presiona "Digitalizar"
    UI->>VM: IniciarDigitalizacionAsync()
    VM->>VM: EstaEscaneando = true
    VM-->>UI: Actualiza barra de progreso

    VM->>DS: DigitalizarAsync() [Task.Run → hilo fondo]

    DS->>HW: CaptureAsync() [STA Thread · COM]
    HW-->>DS: byte[][] páginas en memoria RAM

    DS->>DS: ProcesarImagenesAsync() [paralelo]
    DS->>DS: GenerarPdfA() [iText7 · ICC sRGB]
    DS->>DS: CalcularHashSha256()

    DS->>NAS: Escritura atómica (.tmp → .pdf)
    NAS-->>DS: Ruta UNC confirmada

    DS->>DS: documento.SetHash()
    Note over DS: EstadoVector |= (1 << 4) ✅

    DS-->>VM: ResultadoDigitalizacion
    VM-->>UI: IProgress<T> → DispatcherQueue
    UI-->>Usuario: "✓ Sellado — Hash: a3f9b2c1..."
```

---

## 📂 Estructura de Archivos en el Proyecto

```mermaid
graph TD
    ROOT["📁 DSA (Solución)"]

    ROOT --> DOM["📁 DSA.Domain"]
    ROOT --> APP["📁 DSA.Application"]
    ROOT --> INFRA["📁 DSA.Infrastructure"]
    ROOT --> PRES["📁 DSA.Presentation"]

    DOM --> D1["📄 Entities/Documento.cs\nEstadoVector D[11:0]\n+ Audit Trail"]
    DOM --> D2["📄 Interfaces/IDocumentoRepository.cs"]
    DOM --> D3["📄 Interfaces/IStorageService.cs\n+ EliminarSiExisteAsync ← NUEVO"]
    DOM --> D4["📄 Exceptions/DocumentoInvalidoException.cs\n← NUEVO"]

    APP --> A1["📄 Interfaces/IScannerService.cs\n← REEMPLAZAR"]
    APP --> A2["📄 DTOs/ScannerDTOs.cs\n← NUEVO"]
    APP --> A3["📄 DTOs/DigitizationDTOs.cs\n← NUEVO"]
    APP --> A4["📄 Services/DigitizationService.cs\n← REEMPLAZAR"]

    INFRA --> I1["📄 Hardware/ScannerService.cs\n← REEMPLAZAR"]
    INFRA --> I2["📄 Storage/UncStorageService.cs\n+ Escritura atómica ← MODIFICAR"]
    INFRA --> I3["📁 Resources/sRGB_v4_ICC_preference.icc\n← DESCARGAR"]

    PRES --> P1["📄 ViewModels/CapturaViewModel.cs\n+ CancellationToken ← MODIFICAR"]
    PRES --> P2["📄 Extensions/DispatcherQueueExtensions.cs\n← NUEVO"]
    PRES --> P3["📄 App.xaml.cs\n+ Scoped DigitizationService ← MODIFICAR"]

    style D4 fill:#fff3cd,stroke:#ffc107,color:#856404
    style A1 fill:#cce5ff,stroke:#004085,color:#004085
    style A2 fill:#fff3cd,stroke:#ffc107,color:#856404
    style A3 fill:#fff3cd,stroke:#ffc107,color:#856404
    style A4 fill:#cce5ff,stroke:#004085,color:#004085
    style I1 fill:#cce5ff,stroke:#004085,color:#004085
    style I2 fill:#d4edda,stroke:#155724,color:#155724
    style I3 fill:#f8d7da,stroke:#721c24,color:#721c24
    style P1 fill:#d4edda,stroke:#155724,color:#155724
    style P2 fill:#fff3cd,stroke:#ffc107,color:#856404
    style P3 fill:#d4edda,stroke:#155724,color:#155724
    style D3 fill:#d4edda,stroke:#155724,color:#155724
```

#### Leyenda

| Color | Significado |
|---|---|
| 🔵 Azul | Archivo a **reemplazar** completamente |
| 🟡 Amarillo | Archivo **nuevo** que hay que crear |
| 🟢 Verde | Archivo existente a **modificar** (agregar líneas) |
| 🔴 Rojo | Recurso externo a **descargar** |

---

## 📋 Lista de Tareas de Integración

```mermaid
gantt
    title Orden de Integración Recomendado
    dateFormat  X
    axisFormat  Paso %s

    section Preparación
    Instalar paquetes NuGet en DSA.Infrastructure.csproj     :done, p1, 0, 1
    Agregar referencia COM WIA al .csproj                    :done, p2, 1, 2
    Descargar perfil ICC sRGB a Resources/                   :crit, p3, 2, 3

    section Dominio
    Agregar EliminarSiExisteAsync a IStorageService.cs       :p4, 3, 4
    Implementarlo en UncStorageService.cs                    :p5, 4, 5
    Agregar Audit Trail a Documento.cs                       :p6, 5, 6
    Crear DocumentoInvalidoException.cs                      :p7, 6, 7

    section Interfaces y DTOs
    Crear ScannerDTOs.cs                                     :p8, 7, 8
    Reemplazar IScannerService.cs                            :p9, 8, 9
    Crear DigitizationDTOs.cs                                :p10, 9, 10

    section Servicios
    Reemplazar ScannerService.cs                             :p11, 10, 11
    Reemplazar DigitizationService.cs                        :p12, 11, 12

    section Presentación
    Modificar CapturaViewModel.cs                            :p13, 12, 13
    Crear DispatcherQueueExtensions.cs                       :p14, 13, 14
    Modificar App.xaml.cs (Scoped)                           :p15, 14, 15
```

---

## 🔒 Garantías de Seguridad

```mermaid
mindmap
  root((🛡️ Seguridad\ndel Sistema))
    Hardware
      Sin archivos temporales en C:
      Todo en memoria RAM
      COM liberado en finally
    Criptografía
      SHA-256 moderno
      ZeroMemory tras guardar
      Bit SEAL irreversible
    Almacenamiento
      Escritura atómica .tmp→rename
      Rollback si falla SetHash
      Archivo huérfano imposible
    Hilos
      WIA en hilo STA separado
      IProgress marshala al UI Thread
      CancellationToken en todo
    Conformidad
      PDF/A-1b · ISO 19005-1
      Perfil ICC sRGB embebido
      Metadatos XMP obligatorios
```

---

## 📦 Paquetes Requeridos

```mermaid
graph LR
    subgraph PDF["📄 Generación PDF/A"]
        P1["itext7\n8.0.5"]
        P2["itext7.pdfa\n8.0.5"]
        P3["itext7.bouncy-castle-adapter\n8.0.5"]
    end

    subgraph IMG["🖼️ Procesamiento de Imágenes"]
        I1["SkiaSharp\n2.88.8"]
        I2["SkiaSharp.NativeAssets.Win32\n2.88.8"]
    end

    subgraph HW["🖨️ Hardware"]
        H1["Referencia COM · WIA\nGUID: 94A0E369...\nNo es NuGet — se agrega\ncomo COMReference al .csproj"]
    end

    DSA_INFRA["📁 DSA.Infrastructure.csproj"] --> P1
    DSA_INFRA --> P2
    DSA_INFRA --> P3
    DSA_INFRA --> I1
    DSA_INFRA --> I2
    DSA_INFRA --> H1

    style H1 fill:#fff3cd,stroke:#ffc107,color:#856404
```

### Comandos de instalación

```bash
cd DSA.Infrastructure

dotnet add package itext7 --version 8.0.5
dotnet add package itext7.pdfa --version 8.0.5
dotnet add package itext7.bouncy-castle-adapter --version 8.0.5
dotnet add package SkiaSharp --version 2.88.8
dotnet add package SkiaSharp.NativeAssets.Win32 --version 2.88.8
```

---

## ✅ Validación Final

```mermaid
flowchart LR
    BUILD(["🔨 dotnet build\nSin errores"])
    SCAN(["🖨️ Escáner conectado\nDriver WIA instalado"])
    PDF(["📋 PDF generado\nen memoria"])
    VALID(["🔍 veraPDF\n--flavour 1b"])
    SEAL(["🔐 IsSellado = true\nEstadoVector & 0x0010"])
    ARCH(["🗄️ PuedeArchivarse()\nretorna true"])

    BUILD --> SCAN --> PDF --> VALID --> SEAL --> ARCH

    style BUILD fill:#cce5ff,stroke:#004085,color:#004085
    style VALID fill:#fff3cd,stroke:#856404,color:#856404
    style SEAL fill:#d4edda,stroke:#155724,color:#155724
    style ARCH fill:#d4edda,stroke:#155724,color:#155724
```

> **Descarga del validador PDF/A:** <https://verapdf.org>
>
> **Descarga del perfil ICC sRGB:** <https://www.color.org/srgbprofiles.xalter>
