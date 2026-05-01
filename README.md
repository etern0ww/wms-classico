# WMS Classico

Sistema de Gerenciamento de Armazém (Warehouse Management System) desenvolvido em .NET 10.

## 📋 Descrição

O WMS Classico é um sistema para gestão de armazéns com foco em:
- **Gestão de Ilhas**: Controle de ilhas geográficas para armazenamento
- **Rastreamento de Pacotes**: Monitoramento completo de pacotes com eventos de tracking
- **Poka-Yoke**: Sistema de prevenção de erros na operação
- **Triagem Inteligente**: Motor de triagem para decisões automáticas de roteamento

## 🏗️ Arquitetura

```
WmsClassico.slnx
├── WmsClassico.Api/          # API ASP.NET Core
│   ├── Controllers/          # Controladores MVC
│   ├── Domain/               # Entidades e modelos de domínio
│   │   ├── Entities/         # Entidades de negócio
│   │   └── Models/           # Modelos de dados
│   ├── Infrastructure/       # Repositórios e acesso a dados
│   ├── Presentation/         # ViewModels
│   ├── Services/             # Serviços de negócio
│   └── Views/                # Views Razor
└── build-validate/           # Artefatos de build
```

## 🛠️ Tecnologias

- **Backend**: .NET 10, ASP.NET Core
- **Frontend**: Razor Views, Bootstrap
- **Banco de Dados**: SQLite (in-memory disponível)
- **API**: OpenAPI/Swagger

## 🚀 Começando

### Pré-requisitos

- .NET 10 SDK
- Visual Studio 2022+ ou VS Code

### Executando o projeto

```bash
cd WmsClassico.Api
dotnet run
```

A API estará disponível em `https://localhost:7xxx`

### Endpoints principais

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | `/api/islands` | Listar todas as ilhas |
| GET | `/api/packages` | Listar pacotes |
| GET | `/api/packages/{id}` | Detalhes de um pacote |
| POST | `/api/packages/checkin` | Fazer check-in de pacote |
| GET | `/` | Dashboard principal |

## 📁 Estrutura de Diretórios

- `Domain/Entities/` - Entidades: GeographicIsland, IslandSlot, PackageRecord, PackageTrackingEvent
- `Domain/Models/` - Modelos: PackageCheckInRequest, RouteDefinition, TriageDecision, PokaYokeAlert
- `Infrastructure/` - Repositórios: IWarehouseRepository, SqliteWarehouseRepository, InMemoryWarehouseRepository
- `Services/` - Serviços de negócio: IPokaYokeService, ITriageEngine

## 📄 Licença

MIT License