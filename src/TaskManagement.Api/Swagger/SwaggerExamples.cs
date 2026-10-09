using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TaskManagement.Application.DTOs;

namespace TaskManagement.Api.Swagger;

/// <summary>
/// Swagger schema filter for example responses.
/// </summary>
public class SwaggerExamples : ISchemaFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type == typeof(TaskDto))
        {
            schema.Example = new OpenApiObject
            {
                ["id"] = new OpenApiString("550e8400-e29b-41d4-a716-446655440000"),
                ["title"] = new OpenApiString("Sample Task"),
                ["description"] = new OpenApiString("This is a sample task description"),
                ["status"] = new OpenApiString("New"),
                ["createdAt"] = new OpenApiString("2024-10-06T10:00:00Z"),
                ["updatedAt"] = new OpenApiString("2024-10-06T10:00:00Z")
            };
        }

        if (context.Type == typeof(PaginatedResult<TaskDto>))
        {
            schema.Example = new OpenApiObject
            {
                ["page"] = new OpenApiInteger(1),
                ["pageSize"] = new OpenApiInteger(20),
                ["totalCount"] = new OpenApiInteger(100),
                ["totalPages"] = new OpenApiInteger(5),
                ["hasNextPage"] = new OpenApiBoolean(true),
                ["hasPreviousPage"] = new OpenApiBoolean(false),
                ["items"] = new OpenApiArray
                {
                    new OpenApiObject
                    {
                        ["id"] = new OpenApiString("550e8400-e29b-41d4-a716-446655440000"),
                        ["title"] = new OpenApiString("First Task"),
                        ["description"] = new OpenApiString("Description 1"),
                        ["status"] = new OpenApiString("New"),
                        ["createdAt"] = new OpenApiString("2024-10-06T10:00:00Z"),
                        ["updatedAt"] = new OpenApiString("2024-10-06T10:00:00Z")
                    }
                }
            };
        }
    }
}
