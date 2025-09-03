
using Distribuidora_La_Central.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;

namespace Distribuidora_La_Central.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FacturaController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public FacturaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("GetAllFacturas")]
        public IActionResult GetAllFacturas()
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            // Asegúrate de incluir el campo 'estado' en la consulta
            SqlDataAdapter da = new SqlDataAdapter("SELECT codigoFactura, codigoCliente, fecha, totalFactura, saldo, tipo, estado FROM Factura", con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            List<Factura> facturas = new List<Factura>();

            foreach (DataRow row in dt.Rows)
            {
                facturas.Add(new Factura
                {
                    codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                    codigoCliente = Convert.ToInt32(row["codigoCliente"]),
                    fecha = Convert.ToDateTime(row["fecha"]),
                    totalFactura = Convert.ToDecimal(row["totalFactura"]),
                    saldo = Convert.ToDecimal(row["saldo"]),
                    tipo = row["tipo"].ToString(),
                    estado = row["estado"]?.ToString() // Asegúrate que tu modelo Factura tenga esta propiedad
                });
            }

            return Ok(facturas);
        }
        [HttpGet("GetFacturaPorCodigo/{codigo}")]
        public IActionResult GetFacturaPorCodigo(int codigo)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Factura WHERE codigoFactura = @codigo", con);
            da.SelectCommand.Parameters.AddWithValue("@codigo", codigo);

            DataTable dt = new DataTable();
            da.Fill(dt);

            if (dt.Rows.Count == 0)
                return NotFound();

            DataRow row = dt.Rows[0];

            var factura = new Factura
            {
                codigoFactura = Convert.ToInt32(row["codigoFactura"]), // Corregido el typo "codigoFactura"
                codigoCliente = Convert.ToInt32(row["codigoCliente"]),
                fecha = Convert.ToDateTime(row["fecha"]),
                totalFactura = Convert.ToDecimal(row["totalFactura"]),
                saldo = Convert.ToDecimal(row["saldo"]),
                tipo = row["tipo"].ToString(),
                estado = row["estado"]?.ToString()
            };

            return Ok(factura);
        }




        [HttpPost("AgregarFactura")]
        public IActionResult AgregarFactura([FromBody] FacturaConDetalles facturaConDetalles)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                con.Open();
                using SqlTransaction transaction = con.BeginTransaction();

                try
                {
                    // 1. Validar stock antes de procesar la factura
                    foreach (var detalle in facturaConDetalles.Detalles)
                    {
                        if (!ValidarStockDisponible(con, transaction, detalle.codigoProducto, detalle.cantidad))
                        {
                            transaction.Rollback();
                            return BadRequest(new { error = $"Stock insuficiente para el producto {detalle.codigoProducto}" });
                        }
                    }

                    // 2. Insertar la factura
                    int codigoFactura = InsertarFactura(con, transaction, facturaConDetalles.Factura);

                    // 3. Insertar detalles y actualizar inventario
                    foreach (var detalle in facturaConDetalles.Detalles)
                    {
                        // Calcular subtotal
                        detalle.subtotal = detalle.cantidad * detalle.precioUnitario;

                        InsertarDetalleFactura(con, transaction, codigoFactura, detalle);
                        ActualizarInventario(con, transaction, detalle.codigoProducto, detalle.cantidad);
                    }

                    // 4. Si es crédito, registrar en tabla de créditos
                    if (facturaConDetalles.Factura.tipo == "Crédito")
                    {
                        RegistrarCredito(con, transaction, codigoFactura, facturaConDetalles.Factura);
                    }

                    transaction.Commit();
                    return Ok(new { codigoFactura, message = "Factura registrada exitosamente" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, new { error = "Error al registrar la factura", details = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error de conexión", details = ex.Message });
            }
        }

        #region Métodos Auxiliares

        private bool ValidarStockDisponible(SqlConnection con, SqlTransaction transaction, int codigoProducto, int cantidadRequerida)
        {
            string query = "SELECT cantidad FROM Producto WHERE codigoProducto = @codigoProducto";
            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@codigoProducto", codigoProducto);

            int stockActual = Convert.ToInt32(cmd.ExecuteScalar());
            return stockActual >= cantidadRequerida;
        }

        private int InsertarFactura(SqlConnection con, SqlTransaction transaction, Factura factura)
        {
            string query = @"INSERT INTO Factura 
                          (codigoCliente, fecha, totalFactura, saldo, tipo, estado)
                          OUTPUT INSERTED.codigoFactura
                          VALUES (@codigoCliente, @fecha, @totalFactura, @saldo, @tipo, @estado)";

            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@codigoCliente", factura.codigoCliente);
            cmd.Parameters.AddWithValue("@fecha", factura.fecha);
            cmd.Parameters.AddWithValue("@totalFactura", factura.totalFactura);

            decimal saldoInicial = factura.tipo == "Contado" ? 0 : factura.totalFactura;
            cmd.Parameters.AddWithValue("@saldo", saldoInicial);

            cmd.Parameters.AddWithValue("@tipo", factura.tipo);
            cmd.Parameters.AddWithValue("@estado", factura.tipo == "Contado" ? "Pagado" : "Pendiente");

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private void InsertarDetalleFactura(SqlConnection con, SqlTransaction transaction, int codigoFactura, DetalleFactura detalle)
        {
            string query = @"INSERT INTO DetalleFactura 
                         (codigoFactura, codigoProducto, cantidad, precioUnitario, subtotal)
                         VALUES (@codigoFactura, @codigoProducto, @cantidad, @precioUnitario, @subtotal)";

            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@codigoFactura", codigoFactura);
            cmd.Parameters.AddWithValue("@codigoProducto", detalle.codigoProducto);
            cmd.Parameters.AddWithValue("@cantidad", detalle.cantidad);
            cmd.Parameters.AddWithValue("@precioUnitario", detalle.precioUnitario);
            cmd.Parameters.AddWithValue("@subtotal", detalle.subtotal);

            cmd.ExecuteNonQuery();
        }

        private void ActualizarInventario(SqlConnection con, SqlTransaction transaction, int codigoProducto, int cantidadVendida)
        {
            string query = "UPDATE Producto SET cantidad = cantidad - @cantidad WHERE codigoProducto = @codigoProducto";
            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@codigoProducto", codigoProducto);
            cmd.Parameters.AddWithValue("@cantidad", cantidadVendida);
            cmd.ExecuteNonQuery();
        }

        private void RegistrarCredito(SqlConnection con, SqlTransaction transaction, int codigoFactura, Factura factura)
        {
            DateTime fechaVencimiento = factura.fecha.AddDays(30);

            string query = @"INSERT INTO Credito 
                         (codigoFactura, fechaInicial, fechaFinal, saldoMaximo, estado)
                         VALUES (@codigoFactura, @fechaInicial, @fechaFinal, @saldoMaximo, @estado)";

            using SqlCommand cmd = new SqlCommand(query, con, transaction);
            cmd.Parameters.AddWithValue("@codigoFactura", codigoFactura);
            cmd.Parameters.AddWithValue("@fechaInicial", factura.fecha);
            cmd.Parameters.AddWithValue("@fechaFinal", fechaVencimiento);
            cmd.Parameters.AddWithValue("@saldoMaximo", factura.totalFactura);
            cmd.Parameters.AddWithValue("@estado", "Activo");
            cmd.ExecuteNonQuery();
        }
        #endregion

        // En el controlador FacturaController.cs
        [HttpGet("GetFacturasFiltradas")]
        public IActionResult GetFacturasFiltradas(
            [FromQuery] int? codigoCliente = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] decimal? montoMinimo = null,
            [FromQuery] decimal? montoMaximo = null,
            [FromQuery] string tipo = null,
            [FromQuery] string estado = null,
            [FromQuery] bool? pendientes = null)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

            string query = "SELECT * FROM Factura WHERE 1=1";
            var parameters = new List<SqlParameter>();

            if (codigoCliente.HasValue)
            {
                query += " AND codigoCliente = @codigoCliente";
                parameters.Add(new SqlParameter("@codigoCliente", codigoCliente));
            }

            if (fechaDesde.HasValue)
            {
                query += " AND fecha >= @fechaDesde";
                parameters.Add(new SqlParameter("@fechaDesde", fechaDesde));
            }

            if (fechaHasta.HasValue)
            {
                query += " AND fecha <= @fechaHasta";
                parameters.Add(new SqlParameter("@fechaHasta", fechaHasta));
            }

            if (montoMinimo.HasValue)
            {
                query += " AND totalFactura >= @montoMinimo";
                parameters.Add(new SqlParameter("@montoMinimo", montoMinimo));
            }

            if (montoMaximo.HasValue)
            {
                query += " AND totalFactura <= @montoMaximo";
                parameters.Add(new SqlParameter("@montoMaximo", montoMaximo));
            }

            if (!string.IsNullOrEmpty(tipo))
            {
                query += " AND tipo = @tipo";
                parameters.Add(new SqlParameter("@tipo", tipo));
            }

            if (!string.IsNullOrEmpty(estado))
            {
                query += " AND estado = @estado";
                parameters.Add(new SqlParameter("@estado", estado));
            }

            if (pendientes.HasValue && pendientes.Value)
            {
                query += " AND saldo > 0";
            }

            SqlDataAdapter da = new SqlDataAdapter(query, con);
            da.SelectCommand.Parameters.AddRange(parameters.ToArray());

            DataTable dt = new DataTable();
            da.Fill(dt);

            List<Factura> facturas = new List<Factura>();
            foreach (DataRow row in dt.Rows)
            {
                facturas.Add(new Factura
                {
                    codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                    codigoCliente = Convert.ToInt32(row["codigoCliente"]),
                    fecha = Convert.ToDateTime(row["fecha"]),
                    totalFactura = Convert.ToDecimal(row["totalFactura"]),
                    saldo = Convert.ToDecimal(row["saldo"]),
                    tipo = row["tipo"].ToString(),
                    estado = row["estado"]?.ToString()
                });
            }

            return Ok(facturas);
        }

        [HttpDelete("EliminarFactura/{id}")]
        public IActionResult EliminarFactura(int id)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = "DELETE FROM Factura WHERE codigoFactura = @id";
            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@id", id);
            con.Open();
            int rows = cmd.ExecuteNonQuery();
            return Ok(rows > 0);
        }

        [HttpPut("ActualizarFactura/{id}")]
        public IActionResult ActualizarFactura(int id, [FromBody] Factura factura)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = @"UPDATE Factura SET 
                                codigoCliente = @codigoCliente,
                                fecha = @fecha,
                                totalFactura = @totalFactura,
                                saldo = @saldo,
                                tipo = @tipo
                             WHERE codigoFactura = @codigoFactura";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@codigoFactura", id);
            cmd.Parameters.AddWithValue("@codigoCliente", factura.codigoCliente);
            cmd.Parameters.AddWithValue("@fecha", factura.fecha);
            cmd.Parameters.AddWithValue("@totalFactura", factura.totalFactura);
            cmd.Parameters.AddWithValue("@saldo", factura.saldo);
            cmd.Parameters.AddWithValue("@tipo", factura.tipo);

            con.Open();
            int rows = cmd.ExecuteNonQuery();
            if (rows > 0)
                return Ok(new { message = "Factura actualizada correctamente" });
            else
                return NotFound(new { message = "Factura no encontrada" });
        }

        public class FacturaConDetalles
        {
            public Factura Factura { get; set; }
            public List<DetalleFactura> Detalles { get; set; }
        }

        [HttpGet("GetFacturasPendientes")]
        public IActionResult GetFacturasPendientes()
        {
            using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                string query = @"SELECT * FROM Factura 
                        WHERE saldo > 0 
                        AND (estado IS NULL OR estado != 'Pagado')";

                SqlDataAdapter da = new SqlDataAdapter(query, con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                List<Factura> facturas = new List<Factura>();

                foreach (DataRow row in dt.Rows)
                {
                    facturas.Add(new Factura
                    {
                        codigoFactura = Convert.ToInt32(row["codigoFactura"]),
                        totalFactura = Convert.ToDecimal(row["totalFactura"]),
                        saldo = Convert.ToDecimal(row["saldo"]),
                        estado = row["estado"]?.ToString()
                    });
                }

                return Ok(facturas);
            }
        }
    }
}
