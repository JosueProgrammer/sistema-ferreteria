# Sistema de Ferretería

Sistema de gestión para ferretería desarrollado con ASP.NET Core MVC, Entity Framework Core y PostgreSQL.

## 🚀 Características

- **Gestión de Inventario**: Productos, categorías, unidades de medida, presentaciones y movimientos de inventario
- **Gestión de Clientes**: Registro y control de clientes con límites de crédito
- **Gestión de Proveedores**: Control de proveedores y compras
- **Módulo de Ventas**: Facturación, detalle de ventas y pagos
- **Módulo de Compras**: Registro de compras, detalles y pagos
- **Seguridad**: Sistema de usuarios, roles y permisos
- **Auditoría**: Registro de todas las acciones del sistema
- **Configuración**: Sistema de configuración flexible

## 🛠️ Tecnologías

- **.NET 8.0**
- **ASP.NET Core MVC**
- **Entity Framework Core 8.0**
- **PostgreSQL** (Neon DB)
- **Bootstrap 5**
- **jQuery**

## 📋 Requisitos Previos

- .NET 8.0 SDK
- PostgreSQL 12+ (o cuenta en Neon DB)
- Visual Studio 2022 / VS Code / Rider

## 🔧 Instalación

1. Clonar el repositorio:
```bash
git clone https://github.com/TU_USUARIO/Sistema-Ferreteria.git
cd Sistema-Ferreteria
```

2. Restaurar paquetes NuGet:
```bash
dotnet restore
```

3. Configurar la base de datos:
   - Copiar `appsettings.Development.json.example` a `appsettings.Development.json`
   - Actualizar la cadena de conexión con tus credenciales de PostgreSQL

4. Aplicar migraciones:
```bash
dotnet ef database update
```

5. Ejecutar la aplicación:
```bash
dotnet run
```

## 📁 Estructura del Proyecto

```
Sistema-Ferreteria/
├── Controllers/          # Controladores MVC
├── Data/                 # DbContext y configuración de datos
│   └── Migrations/       # Migraciones de Entity Framework
├── Models/               # Modelos de dominio
│   ├── Seguridad/       # Usuarios, Roles, Permisos
│   ├── Inventario/       # Productos, Categorías, Unidades
│   ├── Clientes/         # Clientes
│   ├── Proveedores/      # Proveedores
│   ├── Ventas/           # Ventas y detalles
│   ├── Compras/          # Compras y detalles
│   └── Configuracion/    # Configuración del sistema
├── Services/             # Lógica de negocio
├── Views/                # Vistas Razor
├── ViewModels/           # ViewModels para vistas
├── DTOs/                 # Data Transfer Objects
└── wwwroot/              # Archivos estáticos
```

## 🗄️ Base de Datos

El proyecto utiliza Code First con Entity Framework Core. El schema inicial está documentado en `schema.sql`.

### Crear una nueva migración:
```bash
dotnet ef migrations add NombreMigracion
```

### Aplicar migraciones:
```bash
dotnet ef database update
```


## 📝 Licencia

Este proyecto es privado y confidencial.

## 👥 Contribuidores

- Josue Bermudez
- Rolando Loasiga
- Norlan Umaña
## 📞 Contacto

Para más información, contacta a bermudezjosue183@gmail.com

