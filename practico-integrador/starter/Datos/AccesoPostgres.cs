using Npgsql;

namespace Datos;

public class AccesoPostgres : IAccesoDatos
{
    
    private const string CnAdmin =
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

    
    private const string Cn =
        "Host=localhost;Port=5432;Database=practico;Username=postgres;Password=postgres";

    
    public void CrearEstructura()
    {
        Console.WriteLine("RF2 — Crear estructura");

        
        using (var cnAdmin = new NpgsqlConnection(CnAdmin))
        {
            cnAdmin.Open();

           
            using var cmdCheck = new NpgsqlCommand(
                "SELECT COUNT(*) FROM pg_database WHERE datname = 'practico'", cnAdmin);
            long existe = (long)(cmdCheck.ExecuteScalar() ?? 0L);

            if (existe == 0)
            {
                
                using var cmdCreate = new NpgsqlCommand("CREATE DATABASE practico", cnAdmin);
                cmdCreate.ExecuteNonQuery();
                Console.WriteLine("Base 'practico' creada.");
            }
            else
            {
                Console.WriteLine("Base 'practico' ya existía.");
            }
        }

        
        using var cn = new NpgsqlConnection(Cn);
        cn.Open();

        string[] ddl =
        [
            
            "DROP TABLE IF EXISTS detalle_pedido",
            "DROP TABLE IF EXISTS pedidos",
            "DROP TABLE IF EXISTS productos",
            "DROP TABLE IF EXISTS categorias",
            "DROP TABLE IF EXISTS clientes",

            
            """
            CREATE TABLE categorias (
                id     SERIAL PRIMARY KEY,
                nombre VARCHAR(100) NOT NULL
            )
            """,
            """
            CREATE TABLE clientes (
                id     SERIAL PRIMARY KEY,
                nombre VARCHAR(100) NOT NULL,
                email  VARCHAR(150) NOT NULL
            )
            """,
            """
            CREATE TABLE productos (
                id           SERIAL PRIMARY KEY,
                nombre       VARCHAR(150) NOT NULL,
                precio       NUMERIC(12,2) NOT NULL,
                stock        INT NOT NULL DEFAULT 0,
                categoria_id INT NOT NULL REFERENCES categorias(id)
            )
            """,
            """
            CREATE TABLE pedidos (
                id         SERIAL PRIMARY KEY,
                cliente_id INT NOT NULL REFERENCES clientes(id),
                fecha      DATE NOT NULL
            )
            """,
            """
            CREATE TABLE detalle_pedido (
                pedido_id       INT NOT NULL REFERENCES pedidos(id),
                producto_id     INT NOT NULL REFERENCES productos(id),
                cantidad        INT NOT NULL,
                precio_unitario NUMERIC(12,2) NOT NULL,
                PRIMARY KEY (pedido_id, producto_id)
            )
            """
        ];

        foreach (var sql in ddl)
        {
            using var cmd = new NpgsqlCommand(sql, cn);
            cmd.ExecuteNonQuery();
        }

        Console.WriteLine("Estructura (5 tablas) creada.");
    }

   
    public void InsertarDatosPrueba()
    {
        Console.WriteLine("RF3 — Insertar datos de prueba");

        using var cn = new NpgsqlConnection(Cn);
        cn.Open();
        using var tx = cn.BeginTransaction();

        try
        {
          
            int idElec  = InsertarCategoria(cn, tx, "Electrónica");
            int idLibros= InsertarCategoria(cn, tx, "Libros");
            int idHogar = InsertarCategoria(cn, tx, "Hogar");

           
            int idNotebook  = InsertarProducto(cn, tx, "Notebook 14\"",         850_000m, 10, idElec);
            int idMouse     = InsertarProducto(cn, tx, "Mouse inalámbrico",      12_000m, 50, idElec);
            int idTeclado   = InsertarProducto(cn, tx, "Teclado mecánico",       35_000m, 30, idElec);
            int idCleanCode = InsertarProducto(cn, tx, "Clean Code",             28_000m, 20, idLibros);
            int idLampara   = InsertarProducto(cn, tx, "Lámpara LED escritorio", 15_000m, 25, idHogar);

            
            int idAna  = InsertarCliente(cn, tx, "Ana García",   "ana@email.com");
            int idCarlos = InsertarCliente(cn, tx, "Carlos López", "carlos@email.com");

           
            int idPed1 = InsertarPedido(cn, tx, idAna,   DateTime.Today);
            int idPed2 = InsertarPedido(cn, tx, idCarlos, DateTime.Today.AddDays(-1));

            
            InsertarDetalle(cn, tx, idPed1, idMouse,    2, 12_000m);
            InsertarDetalle(cn, tx, idPed1, idNotebook, 1, 850_000m);
            InsertarDetalle(cn, tx, idPed1, idTeclado,  1, 35_000m);

            
            InsertarDetalle(cn, tx, idPed2, idCleanCode, 2, 28_000m);
            InsertarDetalle(cn, tx, idPed2, idLampara,   1, 15_000m);

            tx.Commit();
            Console.WriteLine("Datos de prueba insertados (commit).");
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void EjecutarOperaciones()
    {
        Console.WriteLine("RF4 — Ejecutar operaciones (C1, C2, U1, D1)");

        using var cn = new NpgsqlConnection(Cn);
        cn.Open();
        using var tx = cn.BeginTransaction();

        try
        {
            
            Console.WriteLine("[C1] Productos con su categoría:");
            const string sqlC1 = """
                SELECT p.id, p.nombre, p.precio, c.nombre AS categoria
                FROM productos p
                INNER JOIN categorias c ON c.id = p.categoria_id
                ORDER BY p.id
                """;
            using (var cmd = new NpgsqlCommand(sqlC1, cn, tx))
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                    Console.WriteLine($"  #{dr["id"]} {dr["nombre"]} — ${dr["precio"]:F2} [{dr["categoria"]}]");
            }

            
            Console.WriteLine("[C2] Detalle y total del pedido #1:");
            const string sqlC2 = """
                SELECT p.nombre, dp.cantidad, dp.precio_unitario,
                       dp.cantidad * dp.precio_unitario AS subtotal
                FROM detalle_pedido dp
                INNER JOIN productos p ON p.id = dp.producto_id
                WHERE dp.pedido_id = 1
                ORDER BY p.nombre
                """;
            decimal total = 0;
            using (var cmd = new NpgsqlCommand(sqlC2, cn, tx))
            using (var dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    decimal sub = Convert.ToDecimal(dr["subtotal"]);
                    total += sub;
                    Console.WriteLine(
                        $"  {dr["nombre"]} x{dr["cantidad"]} @ ${dr["precio_unitario"]:F2} = ${sub:F2}");
                }
            }

            
            const string sqlTotal = """
                SELECT SUM(cantidad * precio_unitario) AS total
                FROM detalle_pedido
                WHERE pedido_id = 1
                """;
            using (var cmd = new NpgsqlCommand(sqlTotal, cn, tx))
                total = Convert.ToDecimal(cmd.ExecuteScalar());

            Console.WriteLine($"  TOTAL pedido #1: ${total:F2}");

            
            const string sqlU1 =
                "UPDATE productos SET precio = precio * 1.10 WHERE categoria_id = @cat";
            int filasU1;
            using (var cmd = new NpgsqlCommand(sqlU1, cn, tx))
            {
                cmd.Parameters.AddWithValue("@cat", 1);
                filasU1 = cmd.ExecuteNonQuery();
            }
            Console.WriteLine($"[U1] Subí 10% precios de categoría #1 -> {filasU1} filas.");

            
            const string sqlD1 =
                "DELETE FROM detalle_pedido WHERE pedido_id = @ped AND producto_id = @prod";
            int filasD1;
            using (var cmd = new NpgsqlCommand(sqlD1, cn, tx))
            {
                cmd.Parameters.AddWithValue("@ped",  1);
                cmd.Parameters.AddWithValue("@prod", 2);
                filasD1 = cmd.ExecuteNonQuery();
            }
            Console.WriteLine($"[D1] Borré línea (pedido 1, producto 2) -> {filasD1} filas.");

            tx.Commit();
            Console.WriteLine("Operaciones confirmadas (commit).");
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

  
    public void DemostrarRollback()
    {
        Console.WriteLine("RF5 — Demostrar rollback");

        using var cn = new NpgsqlConnection(Cn);
        cn.Open();

        
        decimal precioAntes = LeerPrecioProducto(cn, null, 1);
        Console.WriteLine($"Precio del producto #1 ANTES: ${precioAntes:F2}");

        using var tx = cn.BeginTransaction();
        try
        {
            
            using (var cmd = new NpgsqlCommand(
                "UPDATE productos SET precio = @p WHERE id = @id", cn, tx))
            {
                cmd.Parameters.AddWithValue("@p",  1m);
                cmd.Parameters.AddWithValue("@id", 1);
                cmd.ExecuteNonQuery();
            }
            Console.WriteLine("UPDATE aplicado (precio -> 1) dentro de la transacción.");

            
            throw new Exception("algo salió mal.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            Console.WriteLine($"Excepción capturada -> ROLLBACK. (Error simulado: {ex.Message})");
        }

        
        decimal precioDespues = LeerPrecioProducto(cn, null, 1);
        Console.WriteLine($"Precio del producto #1 DESPUÉS: ${precioDespues:F2}");

        if (precioDespues == precioAntes)
            Console.WriteLine("OK: el rollback funcionó, el dato NO cambió.");
        else
            Console.WriteLine("ERROR: el dato cambió, rollback fallido.");
    }

    
    private static int InsertarCategoria(NpgsqlConnection cn, NpgsqlTransaction tx, string nombre)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO categorias (nombre) VALUES (@n) RETURNING id", cn, tx);
        cmd.Parameters.AddWithValue("@n", nombre);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int InsertarCliente(NpgsqlConnection cn, NpgsqlTransaction tx, string nombre, string email)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO clientes (nombre, email) VALUES (@n, @e) RETURNING id", cn, tx);
        cmd.Parameters.AddWithValue("@n", nombre);
        cmd.Parameters.AddWithValue("@e", email);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int InsertarProducto(NpgsqlConnection cn, NpgsqlTransaction tx,
        string nombre, decimal precio, int stock, int categoriaId)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n,@p,@s,@c) RETURNING id",
            cn, tx);
        cmd.Parameters.AddWithValue("@n", nombre);
        cmd.Parameters.AddWithValue("@p", precio);
        cmd.Parameters.AddWithValue("@s", stock);
        cmd.Parameters.AddWithValue("@c", categoriaId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static int InsertarPedido(NpgsqlConnection cn, NpgsqlTransaction tx, int clienteId, DateTime fecha)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO pedidos (cliente_id, fecha) VALUES (@c, @f) RETURNING id", cn, tx);
        cmd.Parameters.AddWithValue("@c", clienteId);
        cmd.Parameters.AddWithValue("@f", fecha);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void InsertarDetalle(NpgsqlConnection cn, NpgsqlTransaction tx,
        int pedidoId, int productoId, int cantidad, decimal precioUnitario)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO detalle_pedido (pedido_id, producto_id, cantidad, precio_unitario) VALUES (@pe,@pr,@ca,@pu)",
            cn, tx);
        cmd.Parameters.AddWithValue("@pe", pedidoId);
        cmd.Parameters.AddWithValue("@pr", productoId);
        cmd.Parameters.AddWithValue("@ca", cantidad);
        cmd.Parameters.AddWithValue("@pu", precioUnitario);
        cmd.ExecuteNonQuery();
    }

    private static decimal LeerPrecioProducto(NpgsqlConnection cn, NpgsqlTransaction? tx, int id)
    {
        using var cmd = tx is null
            ? new NpgsqlCommand("SELECT precio FROM productos WHERE id = @id", cn)
            : new NpgsqlCommand("SELECT precio FROM productos WHERE id = @id", cn, tx);
        cmd.Parameters.AddWithValue("@id", id);
        return Convert.ToDecimal(cmd.ExecuteScalar());
    }
}
