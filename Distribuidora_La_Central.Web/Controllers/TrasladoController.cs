using Distribuidora_La_Central.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Distribuidora_La_Central.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrasladoController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TrasladoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Obtiene los traslados filtrados por código de bodega origen.
        /// </summary>
        /// <param name="codigoBodegaOrigen">Código de la bodega origen</param>
        /// <returns>Lista de traslados</returns>
        [HttpGet]
        [Route("GetTodosLosTraslados")]
        [Produces("application/json")]
        public ActionResult<List<Traslado>> GetTodosLosTraslados()
        {
            List<Traslado> trasladoList = new List<Traslado>();

            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Traslado", con))
                    {
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Traslado traslado = new Traslado
                                {
                                    idTraslado = Convert.ToInt32(reader["idTraslado"]),
                                    codigoProducto = Convert.ToInt32(reader["codigoProducto"]),
                                    idBodegaOrigen = Convert.ToInt32(reader["idBodegaOrigen"]),
                                    idBodegaDestino = Convert.ToInt32(reader["idBodegaDestino"]),
                                    cantidad = Convert.ToInt32(reader["cantidad"]),
                                    fechaTraslado = reader["fechaTraslado"] == DBNull.Value
                                        ? null
                                        : Convert.ToDateTime(reader["fechaTraslado"]),
                                    realizadoPor = Convert.ToInt32(reader["realizadoPor"]),
                                    estado = reader["estado"].ToString()
                                };

                                trasladoList.Add(traslado);
                            }
                        }
                    }
                }

                return Ok(trasladoList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }


        /// <summary>
        /// Registra un nuevo traslado (sin detalle).
        /// </summary>
        [HttpPost]
        [Route("PostTraslado")]
        public string PostTraslado([FromBody] Traslado traslado)
        {
            Response response = new Response();

            try
            {
                using (SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    using (SqlCommand cmd = new SqlCommand(@"INSERT INTO Traslado 
                (codigoProducto, idBodegaOrigen, idBodegaDestino, cantidad, fechaTraslado, realizadoPor, estado) 
                VALUES 
                (@codigoProducto, @idBodegaOrigen, @idBodegaDestino, @cantidad, @fechaTraslado, @realizadoPor, @estado)", con))
                    {
                        cmd.Parameters.AddWithValue("@codigoProducto", traslado.codigoProducto);
                        cmd.Parameters.AddWithValue("@idBodegaOrigen", traslado.idBodegaOrigen);
                        cmd.Parameters.AddWithValue("@idBodegaDestino", traslado.idBodegaDestino);
                        cmd.Parameters.AddWithValue("@cantidad", traslado.cantidad);
                        cmd.Parameters.AddWithValue("@fechaTraslado", traslado.fechaTraslado ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@realizadoPor", traslado.realizadoPor);
                        cmd.Parameters.AddWithValue("@estado", traslado.estado ?? "");

                        con.Open();
                        int rowsAffected = cmd.ExecuteNonQuery();
                        con.Close();

                        if (rowsAffected > 0)
                        {
                            response.StatusCode = 200;
                            response.ErrorMessage = "Traslado guardado correctamente";
                        }
                        else
                        {
                            response.StatusCode = 100;
                            response.ErrorMessage = "Error al guardar el traslado";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                response.ErrorMessage = "Error interno: " + ex.Message;
            }

            return JsonConvert.SerializeObject(response);
        }
    }
}