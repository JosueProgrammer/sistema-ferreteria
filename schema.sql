-- ============================================
-- SISTEMA FERRETERO INTELIGENTE
-- BASE DE DATOS SIMPLIFICADA Y OPTIMIZADA
-- PostgreSQL 12+
-- ============================================

-- ============================================
-- 1. MÓDULO DE SEGURIDAD Y USUARIOS
-- ============================================

CREATE TABLE Usuarios (
    IdUsuario SERIAL PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Usuario VARCHAR(50) NOT NULL UNIQUE,
    ContraseñaHash VARCHAR(255) NOT NULL,
    Estado BOOLEAN NOT NULL DEFAULT TRUE,
    Eliminado BOOLEAN NOT NULL DEFAULT FALSE,
    FechaCreacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IX_Usuarios_Usuario ON Usuarios(Usuario);
CREATE INDEX IX_Usuarios_Estado ON Usuarios(Estado);

CREATE TABLE Roles (
    IdRol SERIAL PRIMARY KEY,
    Nombre VARCHAR(50) NOT NULL UNIQUE,
    Descripcion VARCHAR(255),
    Estado BOOLEAN NOT NULL DEFAULT TRUE,
    Eliminado BOOLEAN NOT NULL DEFAULT FALSE,
    FechaCreacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Permisos (
    IdPermiso SERIAL PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Modulo VARCHAR(50) NOT NULL,
    Descripcion VARCHAR(255),
    Codigo VARCHAR(50) NOT NULL UNIQUE
);

CREATE INDEX IX_Permisos_Modulo ON Permisos(Modulo);

CREATE TABLE RolPermiso (
    IdRolPermiso SERIAL PRIMARY KEY,
    IdRol INTEGER NOT NULL,
    IdPermiso INTEGER NOT NULL,
    CONSTRAINT FK_RolPermiso_Rol FOREIGN KEY (IdRol) REFERENCES Roles(IdRol) ON DELETE CASCADE,
    CONSTRAINT FK_RolPermiso_Permiso FOREIGN KEY (IdPermiso) REFERENCES Permisos(IdPermiso) ON DELETE CASCADE,
    CONSTRAINT UK_RolPermiso UNIQUE (IdRol, IdPermiso)
);

CREATE TABLE UsuarioRol (
    IdUsuarioRol SERIAL PRIMARY KEY,
    IdUsuario INTEGER NOT NULL,
    IdRol INTEGER NOT NULL,
    FechaAsignacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_UsuarioRol_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario) ON DELETE CASCADE,
    CONSTRAINT FK_UsuarioRol_Rol FOREIGN KEY (IdRol) REFERENCES Roles(IdRol) ON DELETE CASCADE,
    CONSTRAINT UK_UsuarioRol UNIQUE (IdUsuario, IdRol)
);

CREATE TABLE Auditoria (
    IdAuditoria BIGSERIAL PRIMARY KEY,
    IdUsuario INTEGER,
    Modulo VARCHAR(50) NOT NULL,
    Accion VARCHAR(50) NOT NULL,
    Entidad VARCHAR(100),
    IdEntidad INTEGER,
    Descripcion VARCHAR(500),
    IpAddress VARCHAR(50),
    Fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Auditoria_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario)
);

CREATE INDEX IX_Auditoria_Usuario ON Auditoria(IdUsuario);
CREATE INDEX IX_Auditoria_Fecha ON Auditoria(Fecha);
CREATE INDEX IX_Auditoria_Modulo ON Auditoria(Modulo);

-- ============================================
-- 2. MÓDULO DE INVENTARIO
-- ============================================

CREATE TABLE UnidadesMedida (
    IdUnidad SERIAL PRIMARY KEY,
    Codigo VARCHAR(10) NOT NULL UNIQUE,
    Nombre VARCHAR(50) NOT NULL,
    Estado BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE Categorias (
    IdCategoria SERIAL PRIMARY KEY,
    Nombre VARCHAR(100) NOT NULL,
    Descripcion VARCHAR(255),
    Estado BOOLEAN NOT NULL DEFAULT TRUE,
    Eliminado BOOLEAN NOT NULL DEFAULT FALSE,
    FechaCreacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IX_Categorias_Estado ON Categorias(Estado);

CREATE TABLE Productos (
    IdProducto SERIAL PRIMARY KEY,
    Codigo VARCHAR(50) NOT NULL UNIQUE,
    CodigoBarras VARCHAR(50),
    Nombre VARCHAR(200) NOT NULL,
    Descripcion VARCHAR(500),
    IdCategoria INTEGER NOT NULL,
    IdUnidadBase INTEGER NOT NULL,
    StockBase DECIMAL(18,4) NOT NULL DEFAULT 0,
    StockMinimo DECIMAL(18,4) NOT NULL DEFAULT 0,
    PrecioBaseVenta DECIMAL(18,2),
    PrecioBaseCompra DECIMAL(18,2),
    Estado BOOLEAN NOT NULL DEFAULT TRUE,
    Eliminado BOOLEAN NOT NULL DEFAULT FALSE,
    FechaCreacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Producto_Categoria FOREIGN KEY (IdCategoria) REFERENCES Categorias(IdCategoria),
    CONSTRAINT FK_Producto_UnidadBase FOREIGN KEY (IdUnidadBase) REFERENCES UnidadesMedida(IdUnidad),
    CONSTRAINT CHK_Producto_StockMinimo CHECK (StockMinimo >= 0),
    CONSTRAINT CHK_Producto_StockBase CHECK (StockBase >= 0)
);

CREATE INDEX IX_Productos_Codigo ON Productos(Codigo);
CREATE INDEX IX_Productos_CodigoBarras ON Productos(CodigoBarras);
CREATE INDEX IX_Productos_Categoria ON Productos(IdCategoria);
CREATE INDEX IX_Productos_Estado ON Productos(Estado);
CREATE INDEX IX_Productos_StockMinimo ON Productos(StockMinimo) WHERE StockBase <= StockMinimo;

CREATE TABLE Presentaciones (
    IdPresentacion SERIAL PRIMARY KEY,
    IdProducto INTEGER NOT NULL,
    NombrePresentacion VARCHAR(100) NOT NULL,
    IdUnidadPresentacion INTEGER NOT NULL,
    FactorConversion DECIMAL(18,6) NOT NULL,
    PrecioVenta DECIMAL(18,2) NOT NULL,
    PrecioCompra DECIMAL(18,2),
    CodigoBarras VARCHAR(50),
    EsPrincipal BOOLEAN NOT NULL DEFAULT FALSE,
    Estado BOOLEAN NOT NULL DEFAULT TRUE,
    Eliminado BOOLEAN NOT NULL DEFAULT FALSE,
    FechaCreacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_Presentacion_Producto FOREIGN KEY (IdProducto) REFERENCES Productos(IdProducto) ON DELETE CASCADE,
    CONSTRAINT FK_Presentacion_Unidad FOREIGN KEY (IdUnidadPresentacion) REFERENCES UnidadesMedida(IdUnidad),
    CONSTRAINT CHK_Presentacion_Factor CHECK (FactorConversion > 0),
    CONSTRAINT CHK_Presentacion_PrecioVenta CHECK (PrecioVenta >= 0),
    CONSTRAINT UK_Presentacion_Producto_Nombre UNIQUE (IdProducto, NombrePresentacion)
);

CREATE INDEX IX_Presentaciones_Producto ON Presentaciones(IdProducto);
CREATE INDEX IX_Presentaciones_Estado ON Presentaciones(Estado);

CREATE TABLE MovimientosInventario (
    IdMovimiento BIGSERIAL PRIMARY KEY,
    IdProducto INTEGER NOT NULL,
    TipoMovimiento VARCHAR(20) NOT NULL,
    CantidadBase DECIMAL(18,4) NOT NULL,
    Fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Observacion VARCHAR(500),
    IdUsuario INTEGER NOT NULL,
    IdReferencia INTEGER,
    TipoReferencia VARCHAR(50),
    CONSTRAINT FK_Movimiento_Producto FOREIGN KEY (IdProducto) REFERENCES Productos(IdProducto),
    CONSTRAINT FK_Movimiento_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario),
    CONSTRAINT CHK_Movimiento_Tipo CHECK (TipoMovimiento IN ('Entrada', 'Salida', 'Ajuste', 'Merma', 'Corte'))
);

CREATE INDEX IX_Movimientos_Producto ON MovimientosInventario(IdProducto);
CREATE INDEX IX_Movimientos_Fecha ON MovimientosInventario(Fecha);
CREATE INDEX IX_Movimientos_Tipo ON MovimientosInventario(TipoMovimiento);
CREATE INDEX IX_Movimientos_Usuario ON MovimientosInventario(IdUsuario);
CREATE INDEX IX_Movimientos_Referencia ON MovimientosInventario(TipoReferencia, IdReferencia);

-- ============================================
-- 3. MÓDULO DE CLIENTES
-- ============================================

CREATE TABLE Clientes (
    IdCliente SERIAL PRIMARY KEY,
    TipoDocumento VARCHAR(10) NOT NULL,
    NumeroDocumento VARCHAR(20) NOT NULL,
    Nombre VARCHAR(200) NOT NULL,
    Telefono VARCHAR(20),
    Direccion VARCHAR(500),
    LimiteCredito DECIMAL(18,2) NOT NULL DEFAULT 0,
    SaldoActual DECIMAL(18,2) NOT NULL DEFAULT 0,
    DescuentoPorcentaje DECIMAL(5,2) DEFAULT 0,
    Estado BOOLEAN NOT NULL DEFAULT TRUE,
    Eliminado BOOLEAN NOT NULL DEFAULT FALSE,
    FechaCreacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Observaciones TEXT,
    CONSTRAINT UK_Cliente_Documento UNIQUE (TipoDocumento, NumeroDocumento),
    CONSTRAINT CHK_Cliente_LimiteCredito CHECK (LimiteCredito >= 0),
    CONSTRAINT CHK_Cliente_Descuento CHECK (DescuentoPorcentaje >= 0 AND DescuentoPorcentaje <= 100)
);

CREATE INDEX IX_Clientes_Documento ON Clientes(TipoDocumento, NumeroDocumento);
CREATE INDEX IX_Clientes_Nombre ON Clientes(Nombre);
CREATE INDEX IX_Clientes_Estado ON Clientes(Estado);

-- ============================================
-- 4. MÓDULO DE PROVEEDORES
-- ============================================

CREATE TABLE Proveedores (
    IdProveedor SERIAL PRIMARY KEY,
    TipoDocumento VARCHAR(10) NOT NULL,
    NumeroDocumento VARCHAR(20) NOT NULL,
    RazonSocial VARCHAR(200) NOT NULL,
    Telefono VARCHAR(20),
    Email VARCHAR(100),
    Direccion VARCHAR(500),
    ContactoNombre VARCHAR(100),
    PlazoPago INTEGER DEFAULT 0,
    Estado BOOLEAN NOT NULL DEFAULT TRUE,
    Eliminado BOOLEAN NOT NULL DEFAULT FALSE,
    FechaCreacion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Observaciones TEXT,
    CONSTRAINT UK_Proveedor_Documento UNIQUE (TipoDocumento, NumeroDocumento)
);

CREATE INDEX IX_Proveedores_Documento ON Proveedores(TipoDocumento, NumeroDocumento);
CREATE INDEX IX_Proveedores_RazonSocial ON Proveedores(RazonSocial);
CREATE INDEX IX_Proveedores_Estado ON Proveedores(Estado);

-- ============================================
-- 5. MÓDULO DE VENTAS
-- ============================================

CREATE TABLE Ventas (
    IdVenta BIGSERIAL PRIMARY KEY,
    NumeroFactura VARCHAR(20) UNIQUE,
    IdCliente INTEGER,
    Fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaVencimiento TIMESTAMP,
    DescuentoMonto DECIMAL(18,2) DEFAULT 0,
    ImpuestoMonto DECIMAL(18,2) DEFAULT 0,
    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
    TipoPago VARCHAR(20) NOT NULL,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Pendiente',
    IdUsuario INTEGER NOT NULL,
    Observaciones VARCHAR(500),
    Eliminado BOOLEAN NOT NULL DEFAULT FALSE,
    UsuarioAnulacion INTEGER,
    FechaAnulacion TIMESTAMP,
    MotivoAnulacion VARCHAR(500),
    CONSTRAINT FK_Venta_Cliente FOREIGN KEY (IdCliente) REFERENCES Clientes(IdCliente),
    CONSTRAINT FK_Venta_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario),
    CONSTRAINT FK_Venta_UsuarioAnulacion FOREIGN KEY (UsuarioAnulacion) REFERENCES Usuarios(IdUsuario),
    CONSTRAINT CHK_Venta_Total CHECK (Total >= 0),
    CONSTRAINT CHK_Venta_TipoPago CHECK (TipoPago IN ('Contado', 'Credito', 'Mixto'))
);

CREATE INDEX IX_Ventas_NumeroFactura ON Ventas(NumeroFactura);
CREATE INDEX IX_Ventas_Cliente ON Ventas(IdCliente);
CREATE INDEX IX_Ventas_Fecha ON Ventas(Fecha);
CREATE INDEX IX_Ventas_Estado ON Ventas(Estado);
CREATE INDEX IX_Ventas_Usuario ON Ventas(IdUsuario);

CREATE TABLE DetalleVenta (
    IdDetalleVenta BIGSERIAL PRIMARY KEY,
    IdVenta BIGINT NOT NULL,
    IdProducto INTEGER NOT NULL,
    IdPresentacion INTEGER NOT NULL,
    Cantidad DECIMAL(18,4) NOT NULL,
    CantidadBase DECIMAL(18,4) NOT NULL,
    PrecioUnitario DECIMAL(18,2) NOT NULL,
    DescuentoMonto DECIMAL(18,2) DEFAULT 0,
    CONSTRAINT FK_DetalleVenta_Venta FOREIGN KEY (IdVenta) REFERENCES Ventas(IdVenta) ON DELETE CASCADE,
    CONSTRAINT FK_DetalleVenta_Producto FOREIGN KEY (IdProducto) REFERENCES Productos(IdProducto),
    CONSTRAINT FK_DetalleVenta_Presentacion FOREIGN KEY (IdPresentacion) REFERENCES Presentaciones(IdPresentacion),
    CONSTRAINT CHK_DetalleVenta_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CHK_DetalleVenta_Precio CHECK (PrecioUnitario >= 0)
);

CREATE INDEX IX_DetalleVenta_Venta ON DetalleVenta(IdVenta);
CREATE INDEX IX_DetalleVenta_Producto ON DetalleVenta(IdProducto);

CREATE TABLE PagosVenta (
    IdPagoVenta BIGSERIAL PRIMARY KEY,
    IdVenta BIGINT NOT NULL,
    FechaPago TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Monto DECIMAL(18,2) NOT NULL,
    MetodoPago VARCHAR(20) NOT NULL,
    NumeroComprobante VARCHAR(50),
    IdUsuario INTEGER NOT NULL,
    CONSTRAINT FK_PagoVenta_Venta FOREIGN KEY (IdVenta) REFERENCES Ventas(IdVenta) ON DELETE CASCADE,
    CONSTRAINT FK_PagoVenta_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario),
    CONSTRAINT CHK_PagoVenta_Monto CHECK (Monto > 0),
    CONSTRAINT CHK_PagoVenta_Metodo CHECK (MetodoPago IN ('Efectivo', 'Transferencia', 'Tarjeta', 'Cheque', 'Otro'))
);

CREATE INDEX IX_PagosVenta_Venta ON PagosVenta(IdVenta);
CREATE INDEX IX_PagosVenta_Fecha ON PagosVenta(FechaPago);

-- ============================================
-- 6. MÓDULO DE COMPRAS
-- ============================================

CREATE TABLE Compras (
    IdCompra BIGSERIAL PRIMARY KEY,
    NumeroFactura VARCHAR(50),
    IdProveedor INTEGER NOT NULL,
    Fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaVencimiento TIMESTAMP,
    DescuentoMonto DECIMAL(18,2) DEFAULT 0,
    ImpuestoMonto DECIMAL(18,2) DEFAULT 0,
    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Pendiente',
    IdUsuario INTEGER NOT NULL,
    Observaciones VARCHAR(500),
    Eliminado BOOLEAN NOT NULL DEFAULT FALSE,
    UsuarioAnulacion INTEGER,
    FechaAnulacion TIMESTAMP,
    MotivoAnulacion VARCHAR(500),
    CONSTRAINT FK_Compra_Proveedor FOREIGN KEY (IdProveedor) REFERENCES Proveedores(IdProveedor),
    CONSTRAINT FK_Compra_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario),
    CONSTRAINT FK_Compra_UsuarioAnulacion FOREIGN KEY (UsuarioAnulacion) REFERENCES Usuarios(IdUsuario),
    CONSTRAINT CHK_Compra_Total CHECK (Total >= 0)
);

CREATE INDEX IX_Compras_NumeroFactura ON Compras(NumeroFactura);
CREATE INDEX IX_Compras_Proveedor ON Compras(IdProveedor);
CREATE INDEX IX_Compras_Fecha ON Compras(Fecha);
CREATE INDEX IX_Compras_Estado ON Compras(Estado);

CREATE TABLE DetalleCompra (
    IdDetalleCompra BIGSERIAL PRIMARY KEY,
    IdCompra BIGINT NOT NULL,
    IdProducto INTEGER NOT NULL,
    IdPresentacion INTEGER NOT NULL,
    Cantidad DECIMAL(18,4) NOT NULL,
    CantidadBase DECIMAL(18,4) NOT NULL,
    PrecioUnitario DECIMAL(18,2) NOT NULL,
    DescuentoMonto DECIMAL(18,2) DEFAULT 0,
    CONSTRAINT FK_DetalleCompra_Compra FOREIGN KEY (IdCompra) REFERENCES Compras(IdCompra) ON DELETE CASCADE,
    CONSTRAINT FK_DetalleCompra_Producto FOREIGN KEY (IdProducto) REFERENCES Productos(IdProducto),
    CONSTRAINT FK_DetalleCompra_Presentacion FOREIGN KEY (IdPresentacion) REFERENCES Presentaciones(IdPresentacion),
    CONSTRAINT CHK_DetalleCompra_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CHK_DetalleCompra_Precio CHECK (PrecioUnitario >= 0)
);

CREATE INDEX IX_DetalleCompra_Compra ON DetalleCompra(IdCompra);
CREATE INDEX IX_DetalleCompra_Producto ON DetalleCompra(IdProducto);

CREATE TABLE PagosCompra (
    IdPagoCompra BIGSERIAL PRIMARY KEY,
    IdCompra BIGINT NOT NULL,
    FechaPago TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Monto DECIMAL(18,2) NOT NULL,
    MetodoPago VARCHAR(20) NOT NULL,
    NumeroComprobante VARCHAR(50),
    IdUsuario INTEGER NOT NULL,
    CONSTRAINT FK_PagoCompra_Compra FOREIGN KEY (IdCompra) REFERENCES Compras(IdCompra) ON DELETE CASCADE,
    CONSTRAINT FK_PagoCompra_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario),
    CONSTRAINT CHK_PagoCompra_Monto CHECK (Monto > 0)
);

CREATE INDEX IX_PagosCompra_Compra ON PagosCompra(IdCompra);
CREATE INDEX IX_PagosCompra_Fecha ON PagosCompra(FechaPago);

-- ============================================
-- 7. MÓDULO DE LICENCIAS
-- ============================================

CREATE TABLE Licencias (
    IdLicencia SERIAL PRIMARY KEY,
    Codigo VARCHAR(100) NOT NULL UNIQUE,
    TipoLicencia VARCHAR(20) NOT NULL,
    FechaInicio TIMESTAMP NOT NULL,
    FechaFin TIMESTAMP NOT NULL,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Activa',
    NumeroUsuarios INTEGER DEFAULT 1,
    ModulosHabilitados VARCHAR(500),
    FechaActivacion TIMESTAMP,
    FechaUltimaValidacion TIMESTAMP,
    Observaciones VARCHAR(500),
    CONSTRAINT CHK_Licencia_Fechas CHECK (FechaFin > FechaInicio),
    CONSTRAINT CHK_Licencia_Tipo CHECK (TipoLicencia IN ('Demo', 'Basica', 'Premium', 'Enterprise'))
);

CREATE INDEX IX_Licencias_Codigo ON Licencias(Codigo);
CREATE INDEX IX_Licencias_Estado ON Licencias(Estado);
CREATE INDEX IX_Licencias_FechaFin ON Licencias(FechaFin);

CREATE TABLE HistorialLicencias (
    IdHistorial SERIAL PRIMARY KEY,
    IdLicencia INTEGER NOT NULL,
    Accion VARCHAR(50) NOT NULL,
    Fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Observaciones VARCHAR(500),
    IdUsuario INTEGER,
    CONSTRAINT FK_HistorialLicencia_Licencia FOREIGN KEY (IdLicencia) REFERENCES Licencias(IdLicencia),
    CONSTRAINT FK_HistorialLicencia_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario)
);

-- ============================================
-- 8. MÓDULO DE CONFIGURACIÓN
-- ============================================

CREATE TABLE Configuracion (
    IdConfig SERIAL PRIMARY KEY,
    Clave VARCHAR(100) NOT NULL UNIQUE,
    Valor TEXT,
    Tipo VARCHAR(20) NOT NULL DEFAULT 'Texto',
    Modulo VARCHAR(50),
    Descripcion VARCHAR(255)
);

CREATE INDEX IX_Configuracion_Clave ON Configuracion(Clave);
CREATE INDEX IX_Configuracion_Modulo ON Configuracion(Modulo);

-- ============================================
-- 9. MÓDULO DE RESPALDOS
-- ============================================

CREATE TABLE Respaldos (
    IdRespaldo SERIAL PRIMARY KEY,
    NombreArchivo VARCHAR(255) NOT NULL,
    Ruta VARCHAR(500) NOT NULL,
    Tamaño BIGINT,
    Tipo VARCHAR(20) NOT NULL,
    Estado VARCHAR(20) NOT NULL DEFAULT 'Completado',
    Fecha TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    IdUsuario INTEGER,
    CONSTRAINT FK_Respaldo_Usuario FOREIGN KEY (IdUsuario) REFERENCES Usuarios(IdUsuario),
    CONSTRAINT CHK_Respaldo_Tipo CHECK (Tipo IN ('Completo', 'Incremental', 'Manual', 'Automatico'))
);

CREATE INDEX IX_Respaldos_Fecha ON Respaldos(Fecha);
CREATE INDEX IX_Respaldos_Tipo ON Respaldos(Tipo);

-- ============================================
-- 10. TABLA DE SECUENCIAS (Para números de factura)
-- ============================================

CREATE TABLE Secuencias (
    IdSecuencia SERIAL PRIMARY KEY,
    Tipo VARCHAR(50) NOT NULL UNIQUE,
    Serie VARCHAR(10) NOT NULL,
    NumeroActual BIGINT NOT NULL DEFAULT 0,
    CONSTRAINT UK_Secuencias_Tipo_Serie UNIQUE (Tipo, Serie)
);

-- ============================================
-- VISTAS ÚTILES
-- ============================================

-- Vista de Stock Actual
CREATE OR REPLACE VIEW vw_StockActual AS
SELECT 
    p.IdProducto,
    p.Codigo,
    p.Nombre,
    p.IdCategoria,
    c.Nombre AS Categoria,
    p.StockBase,
    p.StockMinimo,
    CASE 
        WHEN p.StockBase <= p.StockMinimo THEN 'Bajo'
        ELSE 'Normal'
    END AS EstadoStock,
    p.PrecioBaseVenta,
    p.PrecioBaseCompra,
    p.Estado
FROM Productos p
INNER JOIN Categorias c ON p.IdCategoria = c.IdCategoria
WHERE p.Eliminado = FALSE;

-- Vista de Ventas Resumen
CREATE OR REPLACE VIEW vw_VentasResumen AS
SELECT 
    v.IdVenta,
    v.NumeroFactura,
    v.Fecha,
    v.IdCliente,
    c.Nombre AS ClienteNombre,
    v.Total,
    v.TipoPago,
    v.Estado,
    u.Nombre AS VendedorNombre,
    COALESCE(SUM(pv.Monto), 0) AS MontoPagado,
    v.Total - COALESCE(SUM(pv.Monto), 0) AS SaldoPendiente
FROM Ventas v
LEFT JOIN Clientes c ON v.IdCliente = c.IdCliente
LEFT JOIN Usuarios u ON v.IdUsuario = u.IdUsuario
LEFT JOIN PagosVenta pv ON v.IdVenta = pv.IdVenta
WHERE v.Eliminado = FALSE
GROUP BY v.IdVenta, v.NumeroFactura, v.Fecha, v.IdCliente, c.Nombre, 
         v.Total, v.TipoPago, v.Estado, u.Nombre;

-- Vista de Compras Resumen
CREATE OR REPLACE VIEW vw_ComprasResumen AS
SELECT 
    c.IdCompra,
    c.NumeroFactura,
    c.Fecha,
    c.IdProveedor,
    p.RazonSocial AS ProveedorNombre,
    c.Total,
    c.Estado,
    u.Nombre AS UsuarioNombre,
    COALESCE(SUM(pc.Monto), 0) AS MontoPagado,
    c.Total - COALESCE(SUM(pc.Monto), 0) AS SaldoPendiente
FROM Compras c
LEFT JOIN Proveedores p ON c.IdProveedor = p.IdProveedor
LEFT JOIN Usuarios u ON c.IdUsuario = u.IdUsuario
LEFT JOIN PagosCompra pc ON c.IdCompra = pc.IdCompra
WHERE c.Eliminado = FALSE
GROUP BY c.IdCompra, c.NumeroFactura, c.Fecha, c.IdProveedor, p.RazonSocial,
         c.Total, c.Estado, u.Nombre;

-- ============================================
-- FUNCIONES Y TRIGGERS ÚTILES
-- ============================================

-- Función para actualizar stock al crear movimiento
CREATE OR REPLACE FUNCTION actualizar_stock_inventario()
RETURNS TRIGGER AS $$
BEGIN
    IF NEW.TipoMovimiento = 'Entrada' THEN
        UPDATE Productos 
        SET StockBase = StockBase + NEW.CantidadBase
        WHERE IdProducto = NEW.IdProducto;
    ELSIF NEW.TipoMovimiento = 'Salida' THEN
        UPDATE Productos 
        SET StockBase = StockBase - NEW.CantidadBase
        WHERE IdProducto = NEW.IdProducto;
    ELSIF NEW.TipoMovimiento IN ('Ajuste', 'Merma', 'Corte') THEN
        -- Para ajustes, la cantidad base es el nuevo valor del stock
        UPDATE Productos 
        SET StockBase = NEW.CantidadBase
        WHERE IdProducto = NEW.IdProducto;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger para actualizar stock
CREATE TRIGGER trg_actualizar_stock
    AFTER INSERT ON MovimientosInventario
    FOR EACH ROW
    EXECUTE FUNCTION actualizar_stock_inventario();

-- Función para generar número de factura
CREATE OR REPLACE FUNCTION generar_numero_factura(
    p_tipo VARCHAR(50),
    p_serie VARCHAR(10)
)
RETURNS VARCHAR(20) AS $$
DECLARE
    v_numero_actual BIGINT;
    v_numero_factura VARCHAR(20);
BEGIN
    -- Obtener o crear secuencia
    INSERT INTO Secuencias (Tipo, Serie, NumeroActual)
    VALUES (p_tipo, p_serie, 1)
    ON CONFLICT (Tipo, Serie) 
    DO UPDATE SET NumeroActual = Secuencias.NumeroActual + 1
    RETURNING NumeroActual INTO v_numero_actual;
    
    -- Si no se actualizó, obtener el valor
    IF v_numero_actual IS NULL THEN
        SELECT NumeroActual INTO v_numero_actual
        FROM Secuencias
        WHERE Tipo = p_tipo AND Serie = p_serie;
        
        UPDATE Secuencias
        SET NumeroActual = NumeroActual + 1
        WHERE Tipo = p_tipo AND Serie = p_serie;
        
        SELECT NumeroActual INTO v_numero_actual
        FROM Secuencias
        WHERE Tipo = p_tipo AND Serie = p_serie;
    END IF;
    
    -- Formatear número: Serie-Numero (ej: F001-000001)
    v_numero_factura := p_serie || '-' || LPAD(v_numero_actual::TEXT, 6, '0');
    
    RETURN v_numero_factura;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- DATOS INICIALES
-- ============================================

-- Insertar unidades de medida básicas
INSERT INTO UnidadesMedida (Codigo, Nombre) VALUES
('UN', 'Unidad'),
('KG', 'Kilogramo'),
('GR', 'Gramo'),
('M', 'Metro'),
('CM', 'Centímetro'),
('L', 'Litro'),
('ML', 'Mililitro'),
('CAJ', 'Caja'),
('BOL', 'Bolsa'),
('PAR', 'Par');

-- Insertar roles básicos
INSERT INTO Roles (Nombre, Descripcion) VALUES
('Administrador', 'Acceso completo al sistema'),
('Vendedor', 'Puede realizar ventas y consultar inventario'),
('Almacenero', 'Gestiona inventario y compras'),
('Gerente', 'Acceso a reportes y configuración');

-- Insertar permisos básicos
INSERT INTO Permisos (Nombre, Modulo, Codigo) VALUES
('Ver Inventario', 'Inventario', 'INVENTARIO_VER'),
('Crear Producto', 'Inventario', 'INVENTARIO_CREAR'),
('Editar Producto', 'Inventario', 'INVENTARIO_EDITAR'),
('Eliminar Producto', 'Inventario', 'INVENTARIO_ELIMINAR'),
('Ver Ventas', 'Ventas', 'VENTAS_VER'),
('Crear Venta', 'Ventas', 'VENTAS_CREAR'),
('Anular Venta', 'Ventas', 'VENTAS_ANULAR'),
('Ver Compras', 'Compras', 'COMPRAS_VER'),
('Crear Compra', 'Compras', 'COMPRAS_CREAR'),
('Ver Reportes', 'Reportes', 'REPORTES_VER'),
('Gestionar Usuarios', 'Usuarios', 'USUARIOS_GESTIONAR'),
('Configurar Sistema', 'Configuracion', 'CONFIG_GESTIONAR');

-- Insertar configuración inicial
INSERT INTO Configuracion (Clave, Valor, Tipo, Modulo, Descripcion) VALUES
('EMPRESA_NOMBRE', 'Mi Ferretería', 'Texto', 'General', 'Nombre de la empresa'),
('EMPRESA_RUC', '', 'Texto', 'General', 'RUC de la empresa'),
('EMPRESA_DIRECCION', '', 'Texto', 'General', 'Dirección de la empresa'),
('IMPUESTO_PORCENTAJE', '18', 'Numero', 'Ventas', 'Porcentaje de impuesto (IGV)'),
('MONEDA', 'PEN', 'Texto', 'General', 'Código de moneda'),
('MONEDA_SIMBOLO', 'S/', 'Texto', 'General', 'Símbolo de moneda'),
('FACTURA_SERIE', 'F001', 'Texto', 'Ventas', 'Serie de facturación'),
('ALERTA_STOCK_MINIMO', 'true', 'Booleano', 'Inventario', 'Activar alertas de stock mínimo'),
('LOGO_URL', '', 'Imagen', 'Personalizacion', 'URL o base64 del logo del negocio'),
('TEMA_COLOR', '#007bff', 'Color', 'Personalizacion', 'Color principal del tema');

-- ============================================
-- COMENTARIOS EN TABLAS (Documentación)
-- ============================================

COMMENT ON TABLE Usuarios IS 'Usuarios del sistema con sus credenciales';
COMMENT ON TABLE Productos IS 'Catálogo de productos con su información básica y stock';
COMMENT ON TABLE Presentaciones IS 'Diferentes presentaciones de un producto con factores de conversión';
COMMENT ON TABLE MovimientosInventario IS 'Historial de todos los movimientos de inventario';
COMMENT ON TABLE Ventas IS 'Cabecera de facturas de venta';
COMMENT ON TABLE DetalleVenta IS 'Detalle de productos vendidos en cada factura';
COMMENT ON TABLE Compras IS 'Cabecera de facturas de compra';
COMMENT ON TABLE DetalleCompra IS 'Detalle de productos comprados en cada factura';
COMMENT ON TABLE Clientes IS 'Clientes del negocio con información de crédito';
COMMENT ON TABLE Proveedores IS 'Proveedores de productos';
COMMENT ON TABLE Licencias IS 'Control de licencias del sistema';
COMMENT ON TABLE Configuracion IS 'Configuración general del sistema';
COMMENT ON TABLE Secuencias IS 'Control de numeración de documentos';
