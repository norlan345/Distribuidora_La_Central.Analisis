using Distribuidora_La_Central.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

[Route("api/[controller]")]
[ApiController]
public class CategoriaProductoController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public CategoriaProductoController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("obtener-todas-categorias")]
    public IActionResult ObtenerTodasCategorias()
    {
        try
        {
            using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));
            SqlDataAdapter da = new("SELECT * FROM CategoriaProducto", con);
            DataTable dt = new DataTable();
            da.Fill(dt);

            List<CategoriaProducto> categoriaList = new();

            foreach (DataRow row in dt.Rows)
            {
                categoriaList.Add(new CategoriaProducto
                {
                    idCategoria = Convert.ToInt32(row["idCategoria"]),
                    nombre = row["nombre"].ToString(),
                    descripcion = row["descripcion"].ToString()
                });
            }

            return Ok(categoriaList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                StatusCode = 500,
                Message = "Error al obtener categorías",
                Error = ex.Message
            });
        }
    }

    [HttpPost("registrar-categoria")]
    public IActionResult RegistrarCategoria([FromBody] CategoriaProducto categoria)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(categoria.nombre))
            {
                return BadRequest(new
                {
                    StatusCode = 400,
                    Message = "El nombre de la categoría es requerido"
                });
            }

            using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));
            con.Open();

            // Verificar si la categoría ya existe
            SqlCommand checkCmd = new(
                "SELECT COUNT(*) FROM CategoriaProducto WHERE nombre = @Nombre",
                con);
            checkCmd.Parameters.AddWithValue("@Nombre", categoria.nombre);
            int exists = (int)checkCmd.ExecuteScalar();

            if (exists > 0)
            {
                return Conflict(new
                {
                    StatusCode = 409,
                    Message = "Ya existe una categoría con este nombre"
                });
            }

            // Insertar nueva categoría
            SqlCommand cmd = new(
                "INSERT INTO CategoriaProducto (nombre, descripcion) " +
                "VALUES (@Nombre, @Descripcion); " +
                "SELECT SCOPE_IDENTITY();",
                con);

            cmd.Parameters.AddWithValue("@Nombre", categoria.nombre);
            cmd.Parameters.AddWithValue("@Descripcion", categoria.descripcion ?? (object)DBNull.Value);

            int newId = Convert.ToInt32(cmd.ExecuteScalar());

            return Ok(new
            {
                StatusCode = 200,
                Message = "Categoría registrada con éxito",
                Data = new
                {
                    idCategoria = newId,
                    nombre = categoria.nombre,
                    descripcion = categoria.descripcion
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                StatusCode = 500,
                Message = "Error al registrar la categoría",
                Error = ex.Message
            });
        }
    }
}