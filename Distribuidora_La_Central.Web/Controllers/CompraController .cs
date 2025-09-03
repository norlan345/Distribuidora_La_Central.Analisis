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
    public class CompraController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CompraController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpPut]
        [Route("MarcarComoPagada/{id}")]
        public IActionResult MarcarComoPagada(int id)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                con.Open();

                string query = @"UPDATE Compra 
                        SET Estado = 'Pagado', 
                            FechaPago = @fechaPago
                        WHERE idCompra = @idCompra";

                using SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@idCompra", id);
                cmd.Parameters.AddWithValue("@fechaPago", DateTime.Now);

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return Ok(new { message = "Compra marcada como pagada correctamente" });
                }
                else
                {
                    return NotFound(new { message = "Compra no encontrada" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error al marcar compra como pagada: {ex.Message}" });
            }
        }


        [HttpGet]
        [Route("GetFilteredCompras")]
        public IActionResult GetFilteredCompras(
    [FromQuery] int? proveedorId = null,
    [FromQuery] string? estado = null,
    [FromQuery] string? fechaInicio = null,
    [FromQuery] string? fechaFin = null)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                con.Open();

                // Construir la consulta base con JOIN para incluir información del proveedor
                string query = @"SELECT c.*, p.nombre AS ProveedorNombre 
                        FROM Compra c
                        LEFT JOIN Proveedor p ON c.idProveedor = p.idProveedor
                        WHERE 1=1";

                // Lista de parámetros
                var parameters = new List<SqlParameter>();

                // Añadir filtros según los parámetros recibidos
                if (proveedorId.HasValue && proveedorId > 0)
                {
                    query += " AND c.idProveedor = @proveedorId";
                    parameters.Add(new SqlParameter("@proveedorId", proveedorId));
                }

                if (!string.IsNullOrEmpty(estado))
                {
                    query += " AND c.Estado = @estado";
                    parameters.Add(new SqlParameter("@estado", estado));
                }

                if (DateTime.TryParse(fechaInicio, out DateTime fechaInicioParsed))
                {
                    query += " AND c.fechaCompra >= @fechaInicio";
                    parameters.Add(new SqlParameter("@fechaInicio", fechaInicioParsed));
                }

                if (DateTime.TryParse(fechaFin, out DateTime fechaFinParsed))
                {
                    // Añadir un día para incluir todas las compras del día final
                    query += " AND c.fechaCompra < @fechaFin";
                    parameters.Add(new SqlParameter("@fechaFin", fechaFinParsed.AddDays(1)));
                }

                query += " ORDER BY c.fechaCompra DESC";

                using SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddRange(parameters.ToArray());

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                List<CompraConProveedor> compras = new List<CompraConProveedor>();

                if (dt.Rows.Count > 0)
                {
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        var compra = new CompraConProveedor
                        {
                            idCompra = Convert.ToInt32(dt.Rows[i]["idCompra"]),
                            idProveedor = Convert.ToInt32(dt.Rows[i]["idProveedor"]),
                            fechaCompra = Convert.ToDateTime(dt.Rows[i]["fechaCompra"]),
                            TotalCompra = Convert.ToDecimal(dt.Rows[i]["TotalCompra"]),
                            Estado = Convert.ToString(dt.Rows[i]["Estado"]),
                            FechaPago = dt.Rows[i]["FechaPago"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[i]["FechaPago"]) : (DateTime?)null,
                            MetodoPago = Convert.ToString(dt.Rows[i]["MetodoPago"]),
                            Proveedor = new ProveedorInfo
                            {
                                idProveedor = Convert.ToInt32(dt.Rows[i]["idProveedor"]),
                                nombre = Convert.ToString(dt.Rows[i]["ProveedorNombre"])
                            }
                        };
                        compras.Add(compra);
                    }
                }

                return Ok(compras);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error al obtener compras filtradas: {ex.Message}" });
            }
        }

        // Clases adicionales para la respuesta
        public class CompraConProveedor : Compra
        {
            public ProveedorInfo Proveedor { get; set; }
        }

        public class ProveedorInfo
        {
            public int idProveedor { get; set; }
            public string nombre { get; set; }
        }
        [HttpGet]
        [Route("GetAllCompras")]
        public IActionResult GetAllCompras()
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                con.Open();

                string query = @"SELECT c.*, p.nombre AS ProveedorNombre 
                         FROM Compra c
                         LEFT JOIN Proveedor p ON c.idProveedor = p.idProveedor
                         ORDER BY c.fechaCompra DESC";

                using SqlCommand cmd = new SqlCommand(query, con);
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                List<CompraConProveedor> compraList = new List<CompraConProveedor>();

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    var compra = new CompraConProveedor
                    {
                        idCompra = Convert.ToInt32(dt.Rows[i]["idCompra"]),
                        idProveedor = Convert.ToInt32(dt.Rows[i]["idProveedor"]),
                        fechaCompra = Convert.ToDateTime(dt.Rows[i]["fechaCompra"]),
                        TotalCompra = Convert.ToDecimal(dt.Rows[i]["TotalCompra"]),
                        Estado = Convert.ToString(dt.Rows[i]["Estado"]),
                        FechaPago = dt.Rows[i]["FechaPago"] != DBNull.Value ? Convert.ToDateTime(dt.Rows[i]["FechaPago"]) : (DateTime?)null,
                        MetodoPago = Convert.ToString(dt.Rows[i]["MetodoPago"]),
                        Proveedor = new ProveedorInfo
                        {
                            idProveedor = Convert.ToInt32(dt.Rows[i]["idProveedor"]),
                            nombre = Convert.ToString(dt.Rows[i]["ProveedorNombre"])
                        }
                    };
                    compraList.Add(compra);
                }

                return Ok(compraList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error al obtener compras: {ex.Message}" });
            }
        }

        //[HttpPost]
        //[Route("AgregarCompra")]
        //public IActionResult AgregarCompra([FromBody] CompraConDetalles compraConDetalles)
        //{
        //    try
        //    {
        //        using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

        //        // Iniciar transacción
        //        con.Open();
        //        using SqlTransaction transaction = con.BeginTransaction();

        //        try
        //        {
        //            // 1. Insertar la compra principal
        //            string queryCompra = @"INSERT INTO Compra (idProveedor, fechaCompra, TotalCompra, Estado, FechaPago, MetodoPago)
        //                         OUTPUT INSERTED.idCompra
        //                         VALUES (@idProveedor, @fechaCompra, @TotalCompra, @Estado, @FechaPago, @MetodoPago)";

        //            int idCompra;

        //            using (SqlCommand cmd = new SqlCommand(queryCompra, con, transaction))
        //            {
        //                cmd.Parameters.AddWithValue("@idProveedor", compraConDetalles.Compra.idProveedor);
        //                cmd.Parameters.AddWithValue("@fechaCompra", compraConDetalles.Compra.fechaCompra);
        //                cmd.Parameters.AddWithValue("@TotalCompra", compraConDetalles.Compra.TotalCompra);
        //                cmd.Parameters.AddWithValue("@Estado", compraConDetalles.Compra.Estado ?? (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@FechaPago", compraConDetalles.Compra.FechaPago ?? (object)DBNull.Value);
        //                cmd.Parameters.AddWithValue("@MetodoPago", compraConDetalles.Compra.MetodoPago ?? (object)DBNull.Value);

        //                idCompra = (int)cmd.ExecuteScalar();
        //            }

        //            // 2. Insertar los detalles de la compra
        //            if (compraConDetalles.Detalles != null && compraConDetalles.Detalles.Any())
        //            {
        //                string queryDetalle = @"INSERT INTO DetalleCompra 
        //                            (IdCompra, CodigoProducto, Cantidad, PrecioUnitario, Subtotal)
        //                            VALUES 
        //                            (@IdCompra, @CodigoProducto, @Cantidad, @PrecioUnitario, @Subtotal)";

        //                foreach (var detalle in compraConDetalles.Detalles)
        //                {
        //                    using (SqlCommand cmdDetalle = new SqlCommand(queryDetalle, con, transaction))
        //                    {
        //                        decimal subtotal = detalle.Cantidad * detalle.PrecioUnitario;

        //                        cmdDetalle.Parameters.AddWithValue("@IdCompra", idCompra);
        //                        cmdDetalle.Parameters.AddWithValue("@CodigoProducto", detalle.CodigoProducto);
        //                        cmdDetalle.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
        //                        cmdDetalle.Parameters.AddWithValue("@PrecioUnitario", detalle.PrecioUnitario);
        //                        cmdDetalle.Parameters.AddWithValue("@Subtotal", subtotal);

        //                        cmdDetalle.ExecuteNonQuery();
        //                    }
        //                }
        //            }

        //            // Commit si todo sale bien
        //            transaction.Commit();
        //            return Ok(new CompraResponse
        //            {
        //                idCompra = idCompra,
        //                message = "Compra registrada exitosamente"
        //            });
        //        }
        //        catch (Exception ex)
        //        {
        //            transaction.Rollback();
        //            return StatusCode(500, new
        //            {
        //                error = $"Error interno del servidor: {ex.Message}"
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Error de conexión: {ex.Message}");
        //    }
        //}


        [HttpPost]
        [Route("AgregarCompra")]
        public IActionResult AgregarCompra([FromBody] CompraConDetalles compraConDetalles)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                con.Open();
                using SqlTransaction transaction = con.BeginTransaction();

                try
                {
                    // 1. Insertar la compra principal
                    string queryCompra = @"INSERT INTO Compra (idProveedor, fechaCompra, TotalCompra, Estado, FechaPago, MetodoPago)
                         OUTPUT INSERTED.idCompra
                         VALUES (@idProveedor, @fechaCompra, @TotalCompra, @Estado, @FechaPago, @MetodoPago)";

                    int idCompra;
                    using (SqlCommand cmd = new SqlCommand(queryCompra, con, transaction))
                    {
                        cmd.Parameters.AddWithValue("@idProveedor", compraConDetalles.Compra.idProveedor);
                        cmd.Parameters.AddWithValue("@fechaCompra", compraConDetalles.Compra.fechaCompra);
                        cmd.Parameters.AddWithValue("@TotalCompra", compraConDetalles.Compra.TotalCompra);
                        cmd.Parameters.AddWithValue("@Estado", compraConDetalles.Compra.Estado ?? "Pendiente");
                        cmd.Parameters.AddWithValue("@FechaPago", compraConDetalles.Compra.FechaPago ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@MetodoPago", compraConDetalles.Compra.MetodoPago ?? (object)DBNull.Value);

                        idCompra = (int)cmd.ExecuteScalar();
                    }

                    // 2. Procesar detalles y actualizar cantidad en productos
                    if (compraConDetalles.Detalles != null && compraConDetalles.Detalles.Any())
                    {
                        string queryDetalle = @"INSERT INTO DetalleCompra 
                        (IdCompra, CodigoProducto, Cantidad, PrecioUnitario, Subtotal)
                        VALUES 
                        (@IdCompra, @CodigoProducto, @Cantidad, @PrecioUnitario, @Subtotal)";

                        // Usamos 'cantidad' que es el campo correcto según tu modelo Producto
                        string queryActualizarCantidad = @"UPDATE Producto 
                                              SET cantidad = cantidad + @Cantidad
                                              WHERE codigoProducto = @CodigoProducto";

                        foreach (var detalle in compraConDetalles.Detalles)
                        {
                            // Validar cantidad
                            if (detalle.Cantidad <= 0)
                            {
                                transaction.Rollback();
                                return BadRequest(new { error = $"La cantidad debe ser mayor que cero para el producto {detalle.CodigoProducto}" });
                            }

                            // Insertar detalle de compra
                            using (SqlCommand cmdDetalle = new SqlCommand(queryDetalle, con, transaction))
                            {
                                decimal subtotal = detalle.Cantidad * detalle.PrecioUnitario;

                                cmdDetalle.Parameters.AddWithValue("@IdCompra", idCompra);
                                cmdDetalle.Parameters.AddWithValue("@CodigoProducto", detalle.CodigoProducto);
                                cmdDetalle.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                                cmdDetalle.Parameters.AddWithValue("@PrecioUnitario", detalle.PrecioUnitario);
                                cmdDetalle.Parameters.AddWithValue("@Subtotal", subtotal);

                                cmdDetalle.ExecuteNonQuery();
                            }

                            // Actualizar cantidad en producto
                            using (SqlCommand cmdCantidad = new SqlCommand(queryActualizarCantidad, con, transaction))
                            {
                                cmdCantidad.Parameters.AddWithValue("@CodigoProducto", detalle.CodigoProducto);
                                cmdCantidad.Parameters.AddWithValue("@Cantidad", detalle.Cantidad);
                                int rowsAffected = cmdCantidad.ExecuteNonQuery();

                                if (rowsAffected == 0)
                                {
                                    transaction.Rollback();
                                    return BadRequest(new { error = $"Producto con código {detalle.CodigoProducto} no encontrado" });
                                }
                            }
                        }
                    }

                    transaction.Commit();
                    return Ok(new
                    {
                        idCompra = idCompra,
                        message = "Compra registrada exitosamente",
                        detallesCount = compraConDetalles.Detalles?.Count ?? 0
                    });
                }
                catch (SqlException sqlEx)
                {
                    transaction.Rollback();
                    return StatusCode(500, new
                    {
                        error = "Error en la base de datos",
                        details = sqlEx.Message,
                        columnError = sqlEx.Errors.Count > 0 ? sqlEx.Errors[0].ToString() : ""
                    });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return StatusCode(500, new
                    {
                        error = $"Error al procesar la compra: {ex.Message}",
                        innerError = ex.InnerException?.Message
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = $"Error de conexión: {ex.Message}",
                    innerError = ex.InnerException?.Message
                });
            }
        }
        // Clases DTO para la solicitud
        public class CompraConDetalles
        {
            public Compra Compra { get; set; }
            public List<DetalleCompraDto> Detalles { get; set; }
        }

        public class DetalleCompraDto
        {
            public int CodigoProducto { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
        }

        [HttpDelete]
        [Route("EliminarCompra/{id}")]
        public IActionResult EliminarCompra(int id)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = "DELETE FROM Compra WHERE idCompra = @id";
            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@id", id);
            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            return Ok(rowsAffected > 0);
        }

        [HttpPut]
        [Route("ActualizarCompra/{id}")]
        public IActionResult ActualizarCompra(int id, [FromBody] Compra compra)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = @"UPDATE Compra SET 
                                idProveedor = @idProveedor,
                                fechaCompra = @fechaCompra,
                                TotalCompra = @TotalCompra,
                                Estado = @Estado,
                                CreadoPor = @CreadoPor,
                                FechaPago = @FechaPago,
                                MetodoPago = @MetodoPago
                             WHERE idCompra = @idCompra";

            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@idCompra", id);
            cmd.Parameters.AddWithValue("@idProveedor", compra.idProveedor);
            cmd.Parameters.AddWithValue("@fechaCompra", compra.fechaCompra);
            cmd.Parameters.AddWithValue("@TotalCompra", compra.TotalCompra);
            cmd.Parameters.AddWithValue("@Estado", compra.Estado ?? (object)DBNull.Value);

            cmd.Parameters.AddWithValue("@FechaPago", compra.FechaPago ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@MetodoPago", compra.MetodoPago ?? (object)DBNull.Value);

            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected > 0)
                return Ok(new { message = "Compra actualizada correctamente." });
            else
                return NotFound(new { message = "Compra no encontrada." });
        }
    }
}