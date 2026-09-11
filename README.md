# Sistema TS REST Middleware

A .NET middleware service that bridges modern JSON REST APIs to the Italian **Sistema Tessera Sanitaria (Sistema TS)** SOAP/MTOM asynchronous 730 expense submission endpoint. Internal systems can submit healthcare expense data via a clean REST interface, and the middleware handles XML generation, schema validation, RSA encryption, ZIP compression, and MTOM SOAP transport.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [API Reference](#api-reference)
- [Pipeline Flow](#pipeline-flow)
- [Encryption Details](#encryption-details)
- [Kit Folder Reference](#kit-folder-reference)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Internal Systems                                  │
│                   (JSON REST requests)                                   │
└─────────────────────────────┬───────────────────────────────────────────┘
                              │ POST /api/v1/sistema-ts/submit
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        SistemaTs.Api                                     │
│              ExpensesController (JSON → DTO)                            │
└─────────────────────────────┬───────────────────────────────────────────┘
                              │ IExpenseService.SubmitAsync()
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    SistemaTs.Infrastructure                              │
│                                                                         │
│  ┌──────────────────┐  ┌────────────────────┐  ┌────────────────────┐  │
│  │ XmlGeneratorSvc  │→ │ XmlValidationSvc   │→ │ SanitelCryptoSvc   │  │
│  │ (DTO → XML)      │  │ (XSD validation)   │  │ (RSA encryption)   │  │
│  └──────────────────┘  └────────────────────┘  └────────────────────┘  │
│                                                            │            │
│  ┌──────────────────┐  ┌────────────────────┐             ▼            │
│  │ ZipCompression   │→ │ SistemaTsSoapClient │────────────────────────►│
│  │ (XML → ZIP)      │  │ (MTOM + BasicAuth) │      Sistema TS Gateway │
│  └──────────────────┘  └────────────────────┘                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Project Structure

```
sistemats-rest-middleware/
│
├── kit730P_ver_20240214/               # Official documentation & XSD files
│   ├── SanitelCF.cer                   # Public certificate for field encryption
│   ├── IndicazioniTecniche.pdf         # Core implementation guide
│   ├── LEGGIMI.txt                     # Directory guide
│   ├── TracciatiWS/                    # XSD schemas and sample payloads
│   │   └── WS_AsincronoInvioDati730/
│   │       └── schemaFileAllegatoInvio/
│   │           └── 730_precompilata.xsd
│   └── SoggettoMedico/                 # Test credentials per category
│       ├── UtenzeTestMedico.txt
│       └── endpointServiziMedico.txt
│
├── src/
│   ├── SistemaTs.Core/                 # Pure domain contracts (no framework deps)
│   │   ├── Dtos/
│   │   │   ├── ExpenseSubmissionRequest.cs
│   │   │   ├── ExpenseRecordDto.cs
│   │   │   ├── ProviderCredentialsDto.cs
│   │   │   ├── SubmissionResultDto.cs
│   │   │   ├── SoapSubmissionRequest.cs
│   │   │   └── XmlValidationResult.cs
│   │   └── Interfaces/
│   │       ├── IExpenseService.cs
│   │       ├── IXmlGeneratorService.cs
│   │       ├── IXmlValidationService.cs
│   │       ├── ICryptoService.cs
│   │       ├── IZipService.cs
│   │       └── ISistemaTsClient.cs
│   │
│   ├── SistemaTs.Infrastructure/       # Protocol implementations
│   │   ├── Configuration/
│   │   │   └── SistemaTsOptions.cs
│   │   ├── Services/
│   │   │   ├── ExpenseService.cs
│   │   │   ├── XmlGeneratorService.cs
│   │   │   ├── XmlValidationService.cs
│   │   │   ├── SanitelCryptoService.cs
│   │   │   ├── ZipCompressionService.cs
│   │   │   └── SistemaTsSoapClient.cs
│   │   ├── DependencyInjection.cs
│   │   ├── SanitelCF.cer               # Bundled certificate
│   │   └── 730_precompilata.xsd        # Bundled XSD schema
│   │
│   └── SistemaTs.Api/                  # ASP.NET Core Web API
│       ├── Controllers/
│       │   └── ExpensesController.cs
│       ├── Program.cs
│       └── appsettings.json
│
├── tests/
│   └── SistemaTs.UnitTests/
│       ├── XmlGeneratorTests.cs
│       ├── CryptoTests.cs
│       └── SoapClientTests.cs
│
├── SistemaTsMiddleware.sln
└── README.md
```

### Project Responsibilities

| Project                      | Role                                                                                                      |
| ---------------------------- | --------------------------------------------------------------------------------------------------------- |
| **SistemaTs.Core**           | Pure C# DTOs and interface contracts. Zero framework dependencies.                                        |
| **SistemaTs.Infrastructure** | Implements XML generation, XSD validation, RSA encryption, ZIP compression, and MTOM SOAP transport.      |
| **SistemaTs.Api**            | ASP.NET Core entry point. Translates JSON to domain workflow and returns HTTP responses.                  |
| **SistemaTs.UnitTests**      | Isolated tests verifying XML structure, encryption, and SOAP payload construction without network access. |

---

## Prerequisites

- .NET SDK 8.0+ (currently built with .NET 10.0)
- Network access to `https://invioSS730pTest.sanita.finanze.it` (test environment)
- Valid test credentials from the Sistema TS kit

---

## Getting Started

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project src/SistemaTs.Api
```

The API starts on the port configured in `Properties/launchSettings.json` (default: `http://localhost:8080`).

### Run Tests

```bash
dotnet test
```

---

## Configuration

Configuration is managed via `appsettings.json` under the `SistemaTs` section:

```json
{
  "SistemaTs": {
    "EndpointUrl": "https://invioSS730pTest.sanita.finanze.it/InvioTelematicoSS730pMtomWeb/InvioTelematicoSS730pMtomPort",
    "CertificatePath": "SanitelCF.cer",
    "XsdSchemaPath": "730_precompilata.xsd"
  }
}
```

| Key               | Description                                                    | Default                |
| ----------------- | -------------------------------------------------------------- | ---------------------- |
| `EndpointUrl`     | SOAP endpoint URL (test or production)                         | Test endpoint          |
| `CertificatePath` | Path to SanitelCF.cer (resolved relative to output dir)        | `SanitelCF.cer`        |
| `XsdSchemaPath`   | Path to 730_precompilata.xsd (resolved relative to output dir) | `730_precompilata.xsd` |

### Endpoints

| Environment    | URL                                                                                                    |
| -------------- | ------------------------------------------------------------------------------------------------------ |
| **Test**       | `https://invioSS730pTest.sanita.finanze.it/InvioTelematicoSS730pMtomWeb/InvioTelematicoSS730pMtomPort` |
| **Production** | `https://invioSS730p.sanita.finanze.it/InvioTelematicoSS730pMtomWeb/InvioTelematicoSS730pMtomPort`     |

### Bundled Files

`SanitelCF.cer` and `730_precompilata.xsd` are bundled inside `src/SistemaTs.Infrastructure/` and copied to the output directory at build time via `CopyToOutputDirectory="PreserveNewest"`. Path resolution checks:

1. Absolute path (if configured as such)
2. Relative to `AppContext.BaseDirectory` (output directory)
3. Relative to current working directory

---

## API Reference

### POST `/api/v1/sistema-ts/submit`

Submits a batch of healthcare expenses to the Sistema TS gateway.

#### Request Body

```json
{
  "credentials": {
    "username": "PROVAX00X00X000Y",
    "password": "Salve123",
    "pincode": "1234567890"
  },
  "owner": {
    "codiceRegione": "120",
    "codiceAsl": "RM1",
    "codiceSsa": "12345",
    "cfProprietario": "PROVAX00X00X000Y"
  },
  "attachmentName": "invio730.zip",
  "xmlEntryName": "730_precompilata.xml",
  "expenses": [
    {
      "pIva": "00265910661",
      "dataEmissione": "2024-01-15",
      "dispositivo": 1,
      "numDocumento": "1234567",
      "dataPagamento": "2024-01-15",
      "flagPagamentoAnticipato": 1,
      "flagOperazione": "I",
      "cfCittadino": "RSSMRA80A01H501U",
      "pagamentoTracciato": "NO",
      "tipoDocumento": "F",
      "flagOpposizione": "0",
      "items": [
        {
          "tipoSpesa": "SR",
          "flagTipoSpesa": "1",
          "importo": 52.01,
          "aliquotaIva": 10.0
        }
      ]
    }
  ]
}
```

#### Field Reference

##### `credentials` (required)

| Field      | Type   | Description                                                |
| ---------- | ------ | ---------------------------------------------------------- |
| `username` | string | Sender username (fiscal code format)                       |
| `password` | string | Sender password for Basic Auth                             |
| `pincode`  | string | **Plaintext** PIN (encrypted automatically before sending) |

##### `owner` (optional)

| Field            | Type    | Validation      | Description                                                |
| ---------------- | ------- | --------------- | ---------------------------------------------------------- |
| `codiceRegione`  | string? | `[A-Z0-9]{3}`   | Region code                                                |
| `codiceAsl`      | string? | `[A-Z0-9]{3}`   | ASL code                                                   |
| `codiceSsa`      | string? | `[A-Z0-9]{5,6}` | SSA code                                                   |
| `cfProprietario` | string? | max 256         | Owner fiscal code (**plaintext**, encrypted automatically) |

##### `expenses[]` (required, min 1)

| Field                     | Type    | Validation                | Description                                                  |
| ------------------------- | ------- | ------------------------- | ------------------------------------------------------------ |
| `pIva`                    | string  | 11 digits                 | VAT number of the issuer                                     |
| `dataEmissione`           | date    | `yyyy-MM-dd`              | Document issue date                                          |
| `dispositivo`             | int     | 1–999                     | Device number                                                |
| `numDocumento`            | string  | `[A-Za-z0-9_./\\-]{1,20}` | Document number                                              |
| `dataPagamento`           | date    | `yyyy-MM-dd`              | Payment date                                                 |
| `flagPagamentoAnticipato` | int?    | `1`                       | Advance payment flag                                         |
| `flagOperazione`          | string  | `I`, `V`, `R`, `C`        | Operation type (Insert, Variation, Refund, Cancellation)     |
| `cfCittadino`             | string? | max 256                   | Citizen fiscal code (**plaintext**, encrypted automatically) |
| `pagamentoTracciato`      | string? | `SI`, `NO`                | Tracked payment flag                                         |
| `tipoDocumento`           | string? | `F`, `D`                  | Document type (Invoice, Commercial receipt)                  |
| `flagOpposizione`         | string? | `0`, `1`                  | Opposition flag                                              |
| `items[]`                 | array   | min 1                     | Expense line items                                           |

##### `expenses[].items[]`

| Field           | Type     | Validation                                                             | Description                                             |
| --------------- | -------- | ---------------------------------------------------------------------- | ------------------------------------------------------- |
| `tipoSpesa`     | string   | `TK`, `FC`, `FV`, `AS`, `AD`, `SR`, `CT`, `PI`, `IC`, `AA`, `SV`, `SP` | Expense type code                                       |
| `flagTipoSpesa` | string?  | `1`, `2`                                                               | Expense type flag                                       |
| `importo`       | decimal  | 0.01–99999.99                                                          | Amount (2 decimal places)                               |
| `aliquotaIva`   | decimal? | 0.00–100.00                                                            | VAT rate (mutually exclusive with `naturaIva`)          |
| `naturaIva`     | string?  | 2–10 chars                                                             | VAT nature code (mutually exclusive with `aliquotaIva`) |

#### Response

**200 OK** — Submission accepted:

```json
{
  "success": true,
  "protocollo": "PROTO123456",
  "codiceEsito": "0",
  "descrizioneEsito": "File ricevuto con successo",
  "dataAccoglienza": "2024-01-15T10:30:00",
  "nomeFileAllegato": "invio730.zip",
  "dimensioneFileAllegato": "1024",
  "idErrore": null,
  "errors": []
}
```

**422 Unprocessable Entity** — XSD validation failed:

```json
{
  "success": false,
  "errors": [
    "Line 5: The element 'documentoSpesa' has invalid child element..."
  ]
}
```

**502 Bad Gateway** — Remote server rejected the request:

```json
{
  "success": false,
  "errors": ["HTTP 500: <env:Envelope>...</env:Envelope>"]
}
```

---

## Pipeline Flow

The `ExpenseService` orchestrates the following steps:

```
1. Encrypt PIN code        → ICryptoService.EncryptToBase64(pincode)
2. Encrypt owner CF        → ICryptoService.EncryptToBase64(cfProprietario)
3. Generate XML            → IXmlGeneratorService.GenerateXml(request)
4. Validate against XSD    → IXmlValidationService.Validate(xml)
5. Compress to ZIP         → IZipService.CompressToZip(xml, entryName)
6. Send via MTOM SOAP      → ISistemaTsClient.InviaFileAsync(payload)
```

If XSD validation fails at step 4, the pipeline short-circuits and returns validation errors.

### XML Structure Generated

The generated XML conforms to `730_precompilata.xsd`:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<precompilata xsi:noNamespaceSchemaLocation="730_precompilata.xsd"
              xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <proprietario>
    <cfProprietario>ENCRYPTED_BASE64...</cfProprietario>
  </proprietario>
  <documentoSpesa>
    <idSpesa>
      <pIva>00265910661</pIva>
      <dataEmissione>2024-01-15</dataEmissione>
      <numDocumentoFiscale>
        <dispositivo>1</dispositivo>
        <numDocumento>1234567</numDocumento>
      </numDocumentoFiscale>
    </idSpesa>
    <dataPagamento>2024-01-15</dataPagamento>
    <flagOperazione>I</flagOperazione>
    <cfCittadino>ENCRYPTED_BASE64...</cfCittadino>
    <pagamentoTracciato>NO</pagamentoTracciato>
    <tipoDocumento>F</tipoDocumento>
    <flagOpposizione>0</flagOpposizione>
    <voceSpesa>
      <tipoSpesa>SR</tipoSpesa>
      <importo>52.01</importo>
      <aliquotaIVA>10.00</aliquotaIVA>
    </voceSpesa>
  </documentoSpesa>
</precompilata>
```

### MTOM SOAP Envelope

The SOAP client builds a `multipart/related` message with:

- **Part 1**: SOAP envelope (`application/xop+xml`) containing the `inviaFileMtom` request with an `xop:Include` reference to the attachment
- **Part 2**: ZIP file (`application/zip`) referenced by `cid:<nomeFileAllegato>`
- **Auth**: Preemptive `Authorization: Basic <base64(user:pass)>` header

---

## Encryption Details

### Certificate

The `SanitelCF.cer` certificate (RSA 1024-bit public key) is provided by Agenzia delle Entrate and used to encrypt sensitive fields before transmission.

### Encrypted Fields

| Field            | Where                                            | Description                                  |
| ---------------- | ------------------------------------------------ | -------------------------------------------- |
| `pincode`        | SOAP `pincodeInvianteCifrato`                    | Sender PIN, encrypted before SOAP submission |
| `cfProprietario` | XML `<proprietario>` and SOAP `datiProprietario` | Owner fiscal code                            |
| `cfCittadino`    | XML `<documentoSpesa>`                           | Citizen fiscal code (if provided plaintext)  |

### Algorithm

- **Padding**: RSA PKCS#1 v1.5 (`RSAEncryptionPadding.Pkcs1`)
- **Encoding**: UTF-8 input → RSA encrypt → Base64 output
- **Key size**: 1024-bit (128-byte ciphertext output)

> **Note**: The caller provides **plaintext** values for `pincode`, `cfProprietario`, and `cfCittadino`. The middleware handles encryption transparently.

---

## Kit Folder Reference

The `kit730P_ver_20240214/` folder contains official documentation from the Ministry of Economy and Finance:

| Path                                       | Content                                            |
| ------------------------------------------ | -------------------------------------------------- |
| `SanitelCF.cer`                            | Public certificate for encrypting sensitive fields |
| `IndicazioniTecniche.pdf`                  | Core technical implementation guide                |
| `LEGGIMI.txt`                              | Directory overview and MTOM usage notes            |
| `TracciatiWS/WS_AsincronoInvioDati730/`    | WSDL, XSD schemas for async submission             |
| `TracciatiWS/EsempiFileXmlInvioAsincrono/` | Sample XML files per category                      |
| `SoggettoMedico/UtenzeTestMedico.txt`      | Test credentials for medical professionals         |
| `SoggettoFarmacia/UtenzeTestFarmacia.txt`  | Test credentials for pharmacies                    |
| `ValidatoreXml/`                           | Official XML validator tool                        |

### Test Credentials (Medico)

From `kit730P_ver_20240214/SoggettoMedico/UtenzeTestMedico.txt`:

| Field    | Value              |
| -------- | ------------------ |
| Username | `PROVAX00X00X000Y` |
| Password | `Salve123`         |
| Pincode  | `1234567890`       |

---

## Testing

### Unit Tests

```bash
dotnet test
```

Tests cover:

| Test Class          | Coverage                                                                                             |
| ------------------- | ---------------------------------------------------------------------------------------------------- |
| `XmlGeneratorTests` | XML structure, field mapping, multiple expenses, `naturaIVA` vs `aliquotaIVA`                        |
| `CryptoTests`       | RSA encryption output validity, different inputs produce different outputs, PKCS1 padding randomness |
| `SoapClientTests`   | Response parsing, HTTP error handling, Basic Auth header, multipart content type (all mocked)        |

### Integration Testing

To test against the real Sistema TS test environment:

```bash
curl -X POST http://localhost:8080/api/v1/sistema-ts/submit \
  -H "Content-Type: application/json" \
  -d '{
    "credentials": {
      "username": "PROVAX00X00X000Y",
      "password": "Salve123",
      "pincode": "1234567890"
    },
    "owner": { "cfProprietario": "PROVAX00X00X000Y" },
    "expenses": [{
      "pIva": "00265910661",
      "dataEmissione": "2024-01-15",
      "dispositivo": 1,
      "numDocumento": "1234567",
      "dataPagamento": "2024-01-15",
      "flagOperazione": "I",
      "cfCittadino": "RSSMRA80A01H501U",
      "pagamentoTracciato": "NO",
      "tipoDocumento": "F",
      "flagOpposizione": "0",
      "items": [{ "tipoSpesa": "SR", "importo": 52.01, "aliquotaIva": 10.00 }]
    }]
  }'
```

> **Important**: Use valid test credentials from the kit. Invalid credentials will result in HTTP 500 SOAP faults from the gateway.

---

## Troubleshooting

### SSL Certificate Error (`PartialChain`)

The test environment's SSL certificate may have an incomplete chain. The middleware is configured to bypass certificate validation via `SocketsHttpHandler.SslOptions.RemoteCertificateValidationCallback`. This is intentional for the test environment.

### HTTP 500 / `env:Client Internal Error`

This typically indicates:

- Invalid or missing credentials (username/password/pincode)
- Malformed SOAP envelope
- Server-side rejection of the payload

Ensure you use the official test credentials from the kit folder.

### XSD Validation Errors (422)

The middleware validates XML locally before sending. Common issues:

- Missing required fields (`pIva`, `numDocumento`, `flagOperazione`)
- Invalid `tipoSpesa` codes
- `importo` outside range or with wrong decimal format
- Both `aliquotaIva` and `naturaIva` specified (mutually exclusive)

### File Not Found (Certificate/XSD)

The middleware resolves files in this order:

1. Absolute path (if configured)
2. `AppContext.BaseDirectory` (build output)
3. Current working directory

Ensure `SanitelCF.cer` and `730_precompilata.xsd` are present in `src/SistemaTs.Infrastructure/` and the project builds successfully.

---

## Security Notes

- **Never commit real credentials** to source control
- Use environment variables or secrets management for production credentials
- The SSL certificate validation bypass should be removed for production
- The middleware encrypts sensitive fields (PIN, fiscal codes) before transmission
- Basic Auth credentials are sent over HTTPS only

ToStartABuild1
