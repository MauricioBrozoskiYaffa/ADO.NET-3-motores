using Datos;

Motor motor;

if (args.Length > 0)
{
    
    motor = args[0].ToLowerInvariant() switch
    {
        "postgres"   => Motor.Postgres,
        "sqlserver"  => Motor.SqlServer,
        "mysql"      => Motor.MySql,
        var x        => throw new ArgumentException($"Motor desconocido: '{x}'. Usá: postgres | sqlserver | mysql")
    };
}
else
{
   
    Console.WriteLine("Seleccioná el motor de base de datos:");
    Console.WriteLine("  1. PostgreSQL");
    Console.WriteLine("  2. SQL Server");
    Console.WriteLine("  3. MySQL");
    Console.Write("Opción: ");

    motor = Console.ReadLine()?.Trim() switch
    {
        "1" => Motor.Postgres,
        "2" => Motor.SqlServer,
        "3" => Motor.MySql,
        var x => throw new ArgumentException($"Opción inválida: '{x}'")
    };
}


string nombreMotor = motor switch
{
    Motor.Postgres  => "PostgreSQL",
    Motor.SqlServer => "SQL Server",
    Motor.MySql     => "MySQL",
    _               => motor.ToString()
};

Console.WriteLine();
Console.WriteLine($"===== MOTOR: {nombreMotor} =====");
Console.WriteLine();

IAccesoDatos acceso = FabricaDeMotor.Crear(motor);   

acceso.CrearEstructura();        
Console.WriteLine();
acceso.InsertarDatosPrueba();    
Console.WriteLine();
acceso.EjecutarOperaciones();    
Console.WriteLine();
acceso.DemostrarRollback();      

Console.WriteLine();
Console.WriteLine($"===== FIN ({nombreMotor}) =====");
