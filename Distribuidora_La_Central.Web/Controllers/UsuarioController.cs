using Distribuidora_La_Central.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Text.Json.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

[Route("api/[controller]")]
[ApiController]
public class UsuarioController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public UsuarioController(IConfiguration configuration)
    {
        _configuration = configuration;
    }


    [HttpGet("obtener-todos")]
    public string ObtenerTodosUsuarios()
    {
        using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));
        SqlDataAdapter da = new("SELECT * FROM Usuario", con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        List<Usuario> usuariolis = new List<Usuario>();
        Response response = new Response();

        if (dt.Rows.Count > 0)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                Usuario usuario = new Usuario();
                usuario.idUsuario = Convert.ToInt32(dt.Rows[i]["idUsuario"]);
                usuario.nombre = Convert.ToString(dt.Rows[i]["nombre"]);
                usuario.rol = Convert.ToString(dt.Rows[i]["rol"]);
                usuario.codigoAcceso = Convert.ToString(dt.Rows[i]["codigoAcceso"]);
                usuariolis.Add(usuario);
            }
        }

        if (usuariolis.Count > 0)
            return JsonConvert.SerializeObject(usuariolis);
        else
        {
            response.StatusCode = 100;
            response.ErrorMessage = "No data found";
            return JsonConvert.SerializeObject(response);
        }
    }




    [HttpPost("registrar")]
    public IActionResult Registrar([FromBody] Usuario usuario)
    {
        SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection").ToString());

        SqlDataAdapter checkUser = new SqlDataAdapter("SELECT * FROM Usuario WHERE nombre = @nombre", con);
        checkUser.SelectCommand.Parameters.AddWithValue("@nombre", usuario.nombre);

        DataTable dt = new DataTable();
        checkUser.Fill(dt);

        if (dt.Rows.Count > 0)
        {
            return BadRequest("El usuario ya existe");
        }

        SqlCommand cmd = new SqlCommand("INSERT INTO Usuario (nombre, rol, codigoAcceso) VALUES (@nombre, @rol, @codigoAcceso)", con);
        cmd.Parameters.AddWithValue("@nombre", usuario.nombre);
        cmd.Parameters.AddWithValue("@rol", usuario.rol);
        cmd.Parameters.AddWithValue("@codigoAcceso", usuario.codigoAcceso);

        con.Open();
        int i = cmd.ExecuteNonQuery();
        con.Close();

        if (i > 0)
        {
            return Ok("Registro exitoso");
        }
        else
        {
            return StatusCode(500, "Error al registrar usuario");
        }
    }

    [HttpPut]
    [Route("ActualizarUsuario/{id}")]
    public IActionResult ActualizarUsuario(int id, [FromBody] Usuario usuario)
    {
        using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

        string query = @"UPDATE Usuario SET 
                        nombre = @nombre,
                        rol = @rol,
                        codigoAcceso = @codigoAcceso

                    WHERE idUsuario = @idUsuario";

        using SqlCommand cmd = new SqlCommand(query, con);
        cmd.Parameters.AddWithValue("@idUsuario", id);
        cmd.Parameters.AddWithValue("@nombre", usuario.nombre);
        cmd.Parameters.AddWithValue("@rol", usuario.rol);
        cmd.Parameters.AddWithValue("@codigoAcceso", usuario.codigoAcceso);
       

        con.Open();
        int rowsAffected = cmd.ExecuteNonQuery();

        if (rowsAffected > 0)
            return Ok(new { message = "Usuario actualizado correctamente." });
        else
            return NotFound(new { message = "Usuario no encontrado." });
    }


    [HttpDelete("eliminar/{id}")]
    public IActionResult EliminarUsuario(int id)
    {
        try
        {
            using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));
            SqlCommand cmd = new("DELETE FROM Usuario WHERE idUsuario = @id", con);
            cmd.Parameters.AddWithValue("@id", id);

            con.Open();
            int affectedRows = cmd.ExecuteNonQuery();

            if (affectedRows > 0)
                return Ok(new { message = "Usuario eliminado correctamente" });

            return NotFound(new { message = "Usuario no encontrado" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al eliminar usuario: {ex.Message}" });
        }
    }

   
}