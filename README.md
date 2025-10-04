# Portal Académico – Examen Parcial P1

Proyecto ASP.NET Core MVC con autenticación Individual y EF Core (SQLite).  
Incluye modelos **Curso** y **Matrícula**, con restricciones:  
- Código de curso único  
- Créditos > 0  
- HorarioInicio < HorarioFin  
- No más de una matrícula por usuario en el mismo curso  
- No exceder el CupoMáximo  

Se sembraron **3 cursos activos** y un **usuario Coordinador**:  
- Email: `coordinador@demo.com`  
- Password: `Passw0rd!`  

Para ejecutar:  
```bash
dotnet restore
dotnet ef database update
dotnet run

   Catálogo de cursos — Pregunta 2

Rama: `feature/catalogo-cursos`

- Listado de cursos activos con filtros (nombre, créditos, horario).  
- Vista detalle con botón **Inscribirse**.  
- Validaciones: créditos ≥ 0, rango de créditos válido y horario fin ≥ inicio.

  Redis — Pregunta 4

Rama: feature/sesion-redis

- Se guarda en sesión el **último curso visitado** y aparece un botón “Volver al curso {Nombre}” en el layout.  
- Los cursos **activos** se cachean en Redis por **60 segundos**.  
- El cache se **invalida** automáticamente al crear o editar un curso.
-.\redis-server.exe --port 6379
    Pregunta 5 — Panel de Coordinador

Rama: feature/panel-coordinador  

- Rol Coordinador con autorización.  
- Panel `/Coordinador` protegido.  
- CRUD de cursos.  
- Lista de matrículas con Confirmar/Cancelar.  
- Acceso restringido a otros roles.

Pregunta 6 — Despliegue en Render

Rama: deploy/render

Se desplegó el Portal Académico en Render como Web Service usando PostgreSQL y Redis en producción.
Se configuraron las variables:
ASPNETCORE_ENVIRONMENT=Production,
ASPNETCORE_URLS=http://0.0.0.0:${PORT},
ConnectionStrings__DefaultConnection,
Redis__ConnectionString.

Verificado online el correcto funcionamiento de login, catálogo, inscripción y panel del coordinador.
