using AutoMapper;
using CrawlerWebMVCApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CrawlerWebMVCApp.Helpers
{
    public static class AutoMapperConfig
    {
        public static void RegisterMappings()
        {
            #region Product Models Mappings

            Mapper.Initialize(cfg => cfg.CreateMap<ProductModel, SearchProductResult>());
            //sa vezi care e treaba cu linq projections!
            #endregion
            /*
            Mapper.CreateMap<UtilizatoriModel, LoginModel>().ReverseMap();
            Mapper.CreateMap<UtilizatoriModel, RegisterModel>();
            Mapper.CreateMap<RegisterModel, UtilizatoriModel>().ForMember(d => d.Avatar, opt => opt.ResolveUsing(s =>
            {
                if (s.Avatar == null)
                    return null;
                MemoryStream target = new MemoryStream();
                s.Avatar.InputStream.CopyTo(target);
                return target.ToArray();
            }));
            Mapper.CreateMap<UtilizatoriModel, UserListModel>();

            #endregion

            #region Client Models Mappings

            Mapper.CreateMap<ClientModel, EditClientModel>().ForMember(d => d.Activ, opt => opt.MapFrom(s =>
                    s.Activ == (byte)ActiveState.Active ? true : false
                )).ReverseMap().ForMember(d => d.Activ, opt => opt.MapFrom(s =>
                    s.Activ == true ? (byte)1 : (byte)0
                ));

            Mapper.CreateMap<ClientModel, ClientListModel>().ForMember(d => d.Activ, opt => opt.MapFrom(s =>
                   s.Activ == (byte)ActiveState.Active ? true : false
               )).ReverseMap().ForMember(d => d.Activ, opt => opt.MapFrom(s =>
                   s.Activ == true ? (byte)1 : (byte)0
               ));
            */           
        }
    }
}