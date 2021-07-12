using AutoMapper;
using WebAPI.Services;

namespace WebAPI
{
    public class MapProfile : Profile
    {
        public MapProfile()
        {
            CreateMap<User, UserModel>();
        }
    }
}