using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.Shared;

namespace Tests.Infrastructure;

/// <summary>
/// Проверка статуса и разбор тела ответа WebApi: успех — <see cref="ApiSuccessResponse{T}"/>, ошибка — Problem Details.
/// </summary>
public static class ApiAssert
{
    public static async Task<ApiSuccessResponse<T>> ReadSuccessAsync<T>(HttpResponseMessage response) where T : class
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiSuccessResponse<T>>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        return body;
    }

    public static async Task<ProblemDetails> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Equal((int)expectedStatus, problem.Status);
        return problem;
    }
}
