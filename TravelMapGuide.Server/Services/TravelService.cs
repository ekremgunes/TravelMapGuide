﻿using AutoMapper;
using FluentValidation;
using System.Text.RegularExpressions;
using TravelMapGuideWebApi.Server.Data.Entities;
using TravelMapGuideWebApi.Server.Data.Repositories.Abstract;
using TravelMapGuideWebApi.Server.Helpers;
using TravelMapGuideWebApi.Server.Models;

namespace TravelMapGuideWebApi.Server.Services
{
    public class TravelService : ITravelService
    {
        private readonly IValidator<CreateTravelModel> _createTravelValidator;
        private readonly IValidator<UpdateTravelModel> _updateTravelValidator;
        private readonly ITravelRepository _travelRepository;
        private readonly IMapper _mapper;

        public TravelService(ITravelRepository travelRepository, IValidator<CreateTravelModel> createTravelValidator, IValidator<UpdateTravelModel> updateTravelValidator, IMapper mapper)
        {
            _travelRepository = travelRepository;
            _createTravelValidator = createTravelValidator;
            _updateTravelValidator = updateTravelValidator;
            _mapper = mapper;
        }

        public async Task<Result> CreateAsync(CreateTravelModel model)
        {
            var result = _createTravelValidator.Validate(model);

            if (!result.IsValid)
            {
                var errorMessages = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
                return Result.Failure("Validation error caught.", errorMessages);
            }

            var travel = _mapper.Map<Travel>(model);

            try
            {
                var entity = await _travelRepository.CreateAsync(travel);
                return Result<Travel>.Success(entity, "Travel is Created.");
            }
            catch (Exception e)
            {
                return Result.Failure("An error occurred while creating the travel.", e.Message);
            }
        }
        public async Task<Result<Travel>> UpdateAsync(UpdateTravelModel travelModel)
        {

            var validationResult = await _updateTravelValidator.ValidateAsync(travelModel);
            if (!validationResult.IsValid)
            {
                var errorMessages = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
                return Result<Travel>.Failure("Validation failed.", errorMessages);
            }

            var isExist = await _travelRepository.IsExistById(travelModel.Id);
            if (!isExist)
            {
                return Result<Travel>.Failure("Travel is not found.");
            }

            var updateTravel = _mapper.Map<Travel>(travelModel);

            var result = await _travelRepository.UpdateAsync(updateTravel);
            if (result == null)
            {
                return Result<Travel>.Failure("Travel is not found.");
            }
            return Result<Travel>.Success(result);
        }
        public async Task<Result> DeleteAsync(string id)
        {

            bool isValid = Regex.IsMatch(id, @"^[a-fA-F0-9]{24}$");
            if (string.IsNullOrEmpty(id) || !isValid)
            {
                return Result<Travel>.Failure("Travel is not found. Id not matched.");
            }

            await _travelRepository.DeleteAsync(id);
            return Result.Success();
        }
        public async Task<Result<IEnumerable<Travel>>> GetAllAsync()
        {
            try
            {
                var data = await _travelRepository.GetAllAsync();

                return Result<IEnumerable<Travel>>.Success(data);
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<Travel>>.Failure("An error occurred while getting travels.", ex.Message);
            }
        }
        public async Task<Result<Travel>> GetByIdAsync(string id)
        {

            bool isValid = Regex.IsMatch(id, @"^[a-fA-F0-9]{24}$");
            if (!isValid)
            {
                return Result<Travel>.Failure("Travel is not found. Id not matched.");
            }

            var data = await _travelRepository.GetByIdAsync(id);
            if (data == null)
            {
                return Result<Travel>.Failure("Travel is not found.");
            }
            return Result<Travel>.Success(data);
        }
    }
}