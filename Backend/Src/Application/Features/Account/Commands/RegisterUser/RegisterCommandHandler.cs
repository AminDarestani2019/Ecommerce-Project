﻿using Application.Common.Mapping;
using Application.Dtos.Account;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities.Identity;
using Domain.Enums;
using Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;

namespace Application.Features.Account.Commands.RegisterUser
{
    public class RegisterCommand:IRequest<UserDto>,IMapFrom<User>
    {
        public string PhoneNumber { get; set; }
        public string DisplayName { get; set; }
        public string Password { get; set; }
        public void Mapping(Profile profile)
        {
            profile.CreateMap<RegisterCommand, User>()
                .ForMember(x=>x.UserName,
                c=>c.MapFrom(v=>v.PhoneNumber));
        }
    }
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, UserDto>
    { 
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly ITokenService _tokenService;
        public RegisterCommandHandler(UserManager<User> userManager,IMapper mapper, ITokenService tokenService)
        {
            _mapper = mapper;   
            _userManager = userManager;
            _tokenService = tokenService;
        }

        public async Task<UserDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            var checkUser = await _userManager.Users.AnyAsync(x => x.PhoneNumber == request.PhoneNumber, cancellationToken);
            if (checkUser) throw new BadRequestEntityException("شماره همراه وارد شده تکراری می باشد");

            var user = _mapper.Map<User>(request);
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded) throw new BadRequestEntityException(result.Errors.FirstOrDefault()!.Description);

            var roleResult = await _userManager.AddToRoleAsync(user, RoleType.User.ToString());
            if (!roleResult.Succeeded) throw new BadRequestEntityException(roleResult.Errors.FirstOrDefault()!.Description);

            var mapUser = _mapper.Map<UserDto>(user);
            
            mapUser.Token = await _tokenService.CreateToken(user);
            return mapUser;
        }
    }
}
