﻿using Distribuidora_La_Central.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;

namespace Distribuidora_La_Central.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductoController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ProductoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("GetAllProductos")]
        public IActionResult GetProductos()
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlDataAdapter da = new SqlDataAdapter("SELECT codigoProducto, descripcion, cantidad, costo, items, idProveedor, idCategoria, descuento, idBodega, unidadMedida, margenGanancia FROM Producto", con);
                DataTable dt = new DataTable();
                da.Fill(dt);

                List<Producto> productoList = new List<Producto>();

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        Producto producto = new Producto
                        {
                            codigoProducto = row["codigoProducto"] != DBNull.Value ? Convert.ToInt32(row["codigoProducto"]) : 0,
                            descripcion = row["descripcion"] != DBNull.Value ? Convert.ToString(row["descripcion"]) : string.Empty,
                            cantidad = row["cantidad"] != DBNull.Value ? Convert.ToInt32(row["cantidad"]) : 0,
                            costo = row["costo"] != DBNull.Value ? Convert.ToDecimal(row["costo"]) : 0m,
                            items = row["items"] != DBNull.Value ? Convert.ToInt32(row["items"]) : 0,
                            idProveedor = row["idProveedor"] != DBNull.Value ? Convert.ToInt32(row["idProveedor"]) : 0,
                            idCategoria = row["idCategoria"] != DBNull.Value ? Convert.ToInt32(row["idCategoria"]) : 0,
                            descuento = row["descuento"] != DBNull.Value ? Convert.ToDecimal(row["descuento"]) : 0m,
                            idBodega = row["idBodega"] != DBNull.Value ? Convert.ToInt32(row["idBodega"]) : 0,
                            unidadMedida = row["unidadMedida"] != DBNull.Value ? Convert.ToString(row["unidadMedida"]) : string.Empty,
                            margenGanancia = row["margenGanancia"] != DBNull.Value ? Convert.ToDecimal(row["margenGanancia"]) : 0m

                        };
                        productoList.Add(producto);
                    }
                    return Ok(productoList);
                }
                return NotFound(new { StatusCode = 404, Message = "No se encontraron productos" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { StatusCode = 500, Message = "Error al obtener productos", Error = ex.Message });
            }
        }

        [HttpGet("BuscarProducto")]
        public IActionResult BuscarProducto(
      [FromQuery] int? id = null,
      [FromQuery] int? items = null,
      [FromQuery] string? descripcion = null)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                con.Open();

                string query = "SELECT * FROM Producto WHERE 1=1";
                var parameters = new List<SqlParameter>();

                if (id.HasValue)
                {
                    query += " AND codigoProducto = @id";
                    parameters.Add(new SqlParameter("@id", id));
                }

                if (items.HasValue)
                {
                    query += " AND items = @items";
                    parameters.Add(new SqlParameter("@items", items));
                }

                if (!string.IsNullOrWhiteSpace(descripcion))
                {
                    query += " AND descripcion LIKE @descripcion";
                    parameters.Add(new SqlParameter("@descripcion", $"%{descripcion}%"));
                }

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddRange(parameters.ToArray());

                SqlDataReader reader = cmd.ExecuteReader();

                List<Producto> productos = new List<Producto>();

                while (reader.Read())
                {
                    Producto producto = new Producto
                    {
                        codigoProducto = reader["codigoProducto"] != DBNull.Value ? Convert.ToInt32(reader["codigoProducto"]) : 0,
                        descripcion = reader["descripcion"] != DBNull.Value ? Convert.ToString(reader["descripcion"]) : string.Empty,
                        cantidad = reader["cantidad"] != DBNull.Value ? Convert.ToInt32(reader["cantidad"]) : 0,
                        costo = reader["costo"] != DBNull.Value ? Convert.ToDecimal(reader["costo"]) : 0m,
                        items = reader["items"] != DBNull.Value ? Convert.ToInt32(reader["items"]) : 0,
                        idProveedor = reader["idProveedor"] != DBNull.Value ? Convert.ToInt32(reader["idProveedor"]) : 0,
                        idCategoria = reader["idCategoria"] != DBNull.Value ? Convert.ToInt32(reader["idCategoria"]) : 0,
                        descuento = reader["descuento"] != DBNull.Value ? Convert.ToDecimal(reader["descuento"]) : 0m,
                        idBodega = reader["idBodega"] != DBNull.Value ? Convert.ToInt32(reader["idBodega"]) : 0,
                        unidadMedida = reader["descripcion"] != DBNull.Value ? Convert.ToString(reader["descripcion"]) : string.Empty,
                        margenGanancia = reader["margenGanancia"] != DBNull.Value ? Convert.ToDecimal(reader["margenGanancia"]) : 0m
                    };
                    productos.Add(producto);
                }

                if (productos.Any())
                    return Ok(productos);
                else
                    return NotFound(new { StatusCode = 404, Message = "No se encontraron productos con los criterios proporcionados" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { StatusCode = 500, Message = "Error al buscar productos", Error = ex.Message });
            }
        }


        [HttpPost]
        [Route("RegistrarProducto")]
        public IActionResult RegistrarProducto([FromBody] Producto producto)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

                // Verificar si el producto ya existe
                SqlCommand checkCmd = new SqlCommand(
                    "SELECT COUNT(1) FROM Producto WHERE descripcion = @descripcion AND idProveedor = @idProveedor",
                    con);
                checkCmd.Parameters.AddWithValue("@descripcion", producto.descripcion ?? string.Empty);
                checkCmd.Parameters.AddWithValue("@idProveedor", producto.idProveedor);

                con.Open();
                bool exists = (int)checkCmd.ExecuteScalar() > 0;
                if (exists)
                {
                    return Conflict(new { StatusCode = 409, Message = "El producto ya existe con este proveedor" });
                }

                // Insertar nuevo producto
                SqlCommand insertCmd = new SqlCommand(
                    @"INSERT INTO Producto 
                    (descripcion, cantidad, idCategoria, descuento, costo, idBodega, idProveedor, items, unidadMedida,margenGanancia) 
                    VALUES 
                    (@descripcion, @cantidad, @idCategoria, @descuento, @costo, @idBodega, @idProveedor, @items, @unidadMedida, @margenGanancia);
                    SELECT SCOPE_IDENTITY();",
                    con);

                insertCmd.Parameters.AddWithValue("@descripcion", producto.descripcion ?? string.Empty);
                insertCmd.Parameters.AddWithValue("@cantidad", producto.cantidad);
                insertCmd.Parameters.AddWithValue("@idCategoria", producto.idCategoria);
                insertCmd.Parameters.AddWithValue("@descuento", producto.descuento);
                insertCmd.Parameters.AddWithValue("@costo", producto.costo);
                insertCmd.Parameters.AddWithValue("@idBodega", producto.idBodega);
                insertCmd.Parameters.AddWithValue("@idProveedor", producto.idProveedor);
                insertCmd.Parameters.AddWithValue("@items", producto.items);
                insertCmd.Parameters.AddWithValue("@unidadMedida", producto.unidadMedida ?? string.Empty);
                insertCmd.Parameters.AddWithValue("@margenGanancia", producto.margenGanancia);

                int newId = Convert.ToInt32(insertCmd.ExecuteScalar());

                return Ok(new
                {
                    StatusCode = 200,
                    Message = "Producto registrado exitosamente",
                    ProductoId = newId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    StatusCode = 500,
                    Message = "Error al registrar producto",
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        [HttpPut]
        [Route("ActualizarProducto")]
        public IActionResult ActualizarProducto([FromBody] Producto producto)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

                SqlCommand cmd = new SqlCommand(
                    @"UPDATE Producto SET 
                        descripcion = @descripcion,
                        cantidad = @cantidad,
                        idCategoria = @idCategoria,
                        descuento = @descuento,
                        costo = @costo,
                        idBodega = @idBodega,
                        idProveedor = @idProveedor,
                        items = @items,
unidadMedida = @unidadMedida,
                        margenGanancia = @margenGanancia
                    WHERE codigoProducto = @codigoProducto",
                    con);

                cmd.Parameters.AddWithValue("@codigoProducto", producto.codigoProducto);
                cmd.Parameters.AddWithValue("@descripcion", producto.descripcion ?? string.Empty);
                cmd.Parameters.AddWithValue("@cantidad", producto.cantidad);
                cmd.Parameters.AddWithValue("@idCategoria", producto.idCategoria);
                cmd.Parameters.AddWithValue("@descuento", producto.descuento);
                cmd.Parameters.AddWithValue("@costo", producto.costo);
                cmd.Parameters.AddWithValue("@idBodega", producto.idBodega);
                cmd.Parameters.AddWithValue("@idProveedor", producto.idProveedor);
                cmd.Parameters.AddWithValue("@items", producto.items);
                cmd.Parameters.AddWithValue("@unidadMedida", producto.unidadMedida ?? string.Empty);
                cmd.Parameters.AddWithValue("@margenGanancia", producto.margenGanancia);

                con.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return Ok(new { StatusCode = 200, Message = "Producto actualizado exitosamente" });
                }
                return NotFound(new { StatusCode = 404, Message = "Producto no encontrado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { StatusCode = 500, Message = "Error al actualizar producto", Error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult EliminarProducto(int id)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

                SqlCommand cmd = new SqlCommand("DELETE FROM Producto WHERE codigoProducto = @id", con);
                cmd.Parameters.AddWithValue("@id", id);

                con.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return Ok(new { StatusCode = 200, Message = "Producto eliminado exitosamente" });
                }
                return NotFound(new { StatusCode = 404, Message = "Producto no encontrado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { StatusCode = 500, Message = "Error al eliminar producto", Error = ex.Message });
            }


        }


        [HttpGet("obtener-todas-categorias")]
        public string ObtenerTodasCategorias()
        {
            using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));
            SqlDataAdapter da = new("SELECT * FROM CategoriaProducto", con);
            DataTable dt = new DataTable();
            da.Fill(dt);

            List<CategoriaProducto> categoriaList = new List<CategoriaProducto>();
            Response response = new Response();

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    CategoriaProducto categoria = new CategoriaProducto();
                    categoria.idCategoria = Convert.ToInt32(dt.Rows[i]["idCategoria"]);
                    categoria.nombre = Convert.ToString(dt.Rows[i]["nombre"]);
                    categoria.descripcion = Convert.ToString(dt.Rows[i]["descripcion"]);
                    categoriaList.Add(categoria);
                }
            }

            if (categoriaList.Count > 0)
                return JsonConvert.SerializeObject(categoriaList);
            else
            {
                response.StatusCode = 100;
                response.ErrorMessage = "No se encontraron categorías.";
                return JsonConvert.SerializeObject(response);
            }
        }







    }
}