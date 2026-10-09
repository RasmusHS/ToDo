using AutoMapper;
using ToDo.Application.DTO.Commands.ToDoItem;
using ToDo.Application.DTO.Commands.ToDoList;
using ToDo.Application.DTO.Queries;
using ToDo.Domain;

namespace ToDo.Application.Profiles;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        //CreateMap<, >().ReverseMap();
        CreateMap<ToDoListEntity, ToDoListResponseDto>().ReverseMap();
        CreateMap<ToDoListEntity, QueryToDoListSummaryDto>()
            .ForMember(d => d.ItemCount, o => o.MapFrom(s => s.ToDoItems.Count()));
        CreateMap<ToDoListEntity, CreateToDoListDto>().ReverseMap();
        CreateMap<ToDoListEntity, UpdateToDoListDto>().ReverseMap();
        CreateMap<ToDoListEntity, QueryToDoListDto>().ReverseMap();

        CreateMap<ToDoItemEntity, ToDoItemResponseDto>().ReverseMap();
        CreateMap<ToDoItemEntity, CreateToDoItemDto>().ReverseMap();
        CreateMap<ToDoItemEntity, UpdateToDoItemDto>().ReverseMap();
        CreateMap<ToDoItemEntity, QueryToDoItemDto>().ReverseMap();
    }
}
