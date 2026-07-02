using Api.Dtos;
using Application.Models;

namespace Api.Mapping;

public static class PagedMappers
{
    public static PagedResponseDto<TDto> ToPagedDto<TEntity, TDto>(
        this PagedResult<TEntity> result,
        Func<TEntity, TDto> mapper) =>
        new()
        {
            Items = result.Items.Select(mapper).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
        };
}
