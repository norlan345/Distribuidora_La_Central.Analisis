using Distribuidora_La_Central.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;

[Route("api/[controller]")]
[ApiController]
public class BodegaController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public BodegaController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("obtener-todos")]
    public string ObtenerTodasBodegas()
    {
        using SqlConnection con = new(_configuration.GetConnectionString("DefaultConnection"));
        SqlDataAdapter da = new("SELECT * FROM Bodega", con);
        DataTable dt = new DataTable();
        da.Fill(dt);

        List<Bodega> bodegaList = new List<Bodega>();
        Response response = new Response();

        if (dt.Rows.Count > 0)
        {
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                Bodega bodega = new Bodega();
                bodega.idBodega = Convert.ToInt32(dt.Rows[i]["idBodega"]);
                bodega.nombre = Convert.ToString(dt.Rows[i]["nombre"]);
                bodega.ubicacion = Convert.ToString(dt.Rows[i]["ubicacion"]);

                // Manejo del valor NULL para responsable
                bodega.responsable = dt.Rows[i]["responsable"] == DBNull.Value
                    ? 0  // Valor por defecto cuando es NULL
                    : Convert.ToInt32(dt.Rows[i]["responsable"]);

                // Manejo del valor NULL para fecha
                bodega.fecha = dt.Rows[i]["fecha"] == DBNull.Value
                    ? DateTime.MinValue  // Valor por defecto cuando es NULL
                    : Convert.ToDateTime(dt.Rows[i]["fecha"]);

                bodegaList.Add(bodega);
            }
        }

        if (bodegaList.Count > 0)
            return JsonConvert.SerializeObject(bodegaList);
        else
        {
            response.StatusCode = 100;
            response.ErrorMessage = "No se encontraron bodegas.";
            return JsonConvert.SerializeObject(response);
        }
    }


    [HttpPost("registrar")]
    public IActionResult Registrar([FromBody] Bodega bodega)
    {
        using SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

        // Verificar si ya existe una bodega con el mismo nombre
        SqlDataAdapter checkBodega = new SqlDataAdapter("SELECT * FROM Bodega WHERE nombre = @nombre", con);
        checkBodega.SelectCommand.Parameters.AddWithValue("@nombre", bodega.nombre);

        DataTable dt = new DataTable();
        checkBodega.Fill(dt);

        if (dt.Rows.Count > 0)
        {
            return BadRequest("La bodega ya existe");
        }

        // Insertar la nueva bodega
        SqlCommand cmd = new SqlCommand(@"INSERT INTO Bodega (nombre, ubicacion, responsable, fecha) 
                                      VALUES (@nombre, @ubicacion, @responsable, @fecha)", con);

        cmd.Parameters.AddWithValue("@nombre", bodega.nombre);
        cmd.Parameters.AddWithValue("@ubicacion", bodega.ubicacion);
        cmd.Parameters.AddWithValue("@responsable", bodega.responsable);
        cmd.Parameters.AddWithValue("@fecha", bodega.fecha);

        con.Open();
        int i = cmd.ExecuteNonQuery();
        con.Close();

        if (i > 0)
        {
            return Ok("Bodega registrada exitosamente");
        }
        else
        {
            return StatusCode(500, "Error al registrar bodega");
        }
    }



}