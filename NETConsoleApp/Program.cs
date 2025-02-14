using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NETConnectionDynamics;
//using NETConnectionDGII;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {

            // See https://aka.ms/new-console-template for more information

            //Console.WriteLine("Hello, World!");
            //Console.WriteLine(NETConnectionDGII.ApiClient.Suma(10.5M, 0.5M));

            await RealizarPeticionOData();
            await ObtenerSemilla();
            await FirmarSemilla();
            
        }

        static async Task RealizarPeticionOData()
        {
            try
            {
                // Llamada al método asíncrono de la DLL
                await ApiClient.ObtenerDatosOData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en la petición OData: {ex.Message}");
            }
        }

        static async Task ObtenerSemilla()
        {
            try
            {
                // Llamada al método asíncrono de la DLL
                await ApiClient.ObtenerSemilla();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en la petición Semilla: {ex.Message}");
            }
        }


        static async Task FirmarSemilla()
        {
            try
            {
                // Llamada al método asíncrono de la DLL
                await ApiClient.FirmarSemilla();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en la petición Semilla: {ex.Message}");
            }
        }
    }
}
