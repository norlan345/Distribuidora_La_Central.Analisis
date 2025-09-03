using Distribuidora_La_Central.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json.Serialization;

namespace Distribuidora_La_Central.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClienteController : ControllerBase
    {
        public readonly IConfiguration _configuration;
        public ClienteController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("GetAllClientes")]
        public string GetClientes()
        {
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString());
            SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Cliente;", con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            List<Cliente> clienteList = new List<Cliente>();
            Response response = new Response();

            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    Cliente cliente = new Cliente();
                    cliente.codigoCliente = Convert.ToInt32(dt.Rows[i]["codigoCliente"]);
                    cliente.cedula = Convert.ToString(dt.Rows[i]["cedula"]);
                    cliente.nombre = Convert.ToString(dt.Rows[i]["nombre"]);
                    cliente.apellido = Convert.ToString(dt.Rows[i]["apellido"]);
                    cliente.tipoCliente = Convert.ToString(dt.Rows[i]["tipoCliente"]);
                    cliente.telefono = Convert.ToString(dt.Rows[i]["telefono"]);
                    cliente.direccion = Convert.ToString(dt.Rows[i]["direccion"]);
                    cliente.creado_por = Convert.ToInt32(dt.Rows[i]["creado_por"]);
                    clienteList.Add(cliente);
                }
            }

            if (clienteList.Count > 0)
                return JsonConvert.SerializeObject(clienteList);
            else
            {
                response.StatusCode = 100;
                response.ErrorMessage = "No data found";
                return JsonConvert.SerializeObject(response);
            }








        }

        [HttpPost]
        [Route("AgregarCliente")]
        public IActionResult AgregarCliente([FromBody] Cliente cliente)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = @"INSERT INTO Cliente (cedula, nombre, apellido, tipoCliente, telefono, direccion, creado_por)
                 VALUES (@cedula, @nombre, @apellido, @tipoCliente, @telefono, @direccion, @creado_por)";

            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@cedula", cliente.cedula);
            cmd.Parameters.AddWithValue("@nombre", cliente.nombre);
            cmd.Parameters.AddWithValue("@apellido", cliente.apellido);
            cmd.Parameters.AddWithValue("@tipoCliente", cliente.tipoCliente);
            cmd.Parameters.AddWithValue("@telefono", cliente.telefono);
            cmd.Parameters.AddWithValue("@direccion", cliente.direccion);
            cmd.Parameters.AddWithValue("@creado_por", cliente.creado_por);
            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();
            return Ok(rowsAffected > 0);
        }

        [HttpDelete]
        [Route("EliminarCliente/{id}")]
        public IActionResult EliminarCliente(int id)
        {
            try
            {
                using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                string query = "DELETE FROM Cliente WHERE codigoCliente = @id";
                using SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", id);
                con.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                    return Ok(new { message = "Cliente eliminado correctamente" });
                else
                    return NotFound(new { message = "Cliente no encontrado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al eliminar cliente: {ex.Message}" });
            }
        }



        [HttpPut]
        [Route("ActualizarCliente/{id}")]
        public IActionResult ActualizarCliente(int id, [FromBody] Cliente cliente)
        {
            using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            string query = @"UPDATE Cliente SET 
                        cedula = @cedula,
                        nombre = @nombre,
                        apellido = @apellido,
                        tipoCliente = @tipoCliente,
                        telefono = @telefono,
                        direccion = @direccion,
                        creado_por = @creado_por
                     WHERE codigoCliente = @codigoCliente";

            using SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@codigoCliente", id);
            cmd.Parameters.AddWithValue("@cedula", cliente.cedula);
            cmd.Parameters.AddWithValue("@nombre", cliente.nombre);
            cmd.Parameters.AddWithValue("@apellido", cliente.apellido);
            cmd.Parameters.AddWithValue("@tipoCliente", cliente.tipoCliente);
            cmd.Parameters.AddWithValue("@telefono", cliente.telefono);
            cmd.Parameters.AddWithValue("@direccion", cliente.direccion);
            cmd.Parameters.AddWithValue("@creado_por", cliente.creado_por);
            con.Open();
            int rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected > 0)
                return Ok(new { message = "Cliente actualizado correctamente." });
            else
                return NotFound(new { message = "Cliente no encontrado." });
        }




    }
}