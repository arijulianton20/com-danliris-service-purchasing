using AutoMapper;
using Com.DanLiris.Service.Purchasing.Lib.Enums;
using Com.DanLiris.Service.Purchasing.Lib.Facades.GarmentPurchasingExpedition;
using Com.DanLiris.Service.Purchasing.Lib.Models.GarmentInternNoteModel;
using Com.DanLiris.Service.Purchasing.Lib.Services;
using Com.DanLiris.Service.Purchasing.WebApi.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Com.DanLiris.Service.Purchasing.WebApi.Controllers.v1
{
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/garment-purchasing-expeditions")]
    [Authorize]
    public class GarmentPurchasingExpeditionController : Controller
    {
        private readonly IGarmentPurchasingExpeditionService _service;
        private readonly IdentityService _identityService;
        private readonly IMapper _mapper;
        private const string ApiVersion = "1.0";

        public GarmentPurchasingExpeditionController(IServiceProvider serviceProvider, IMapper mapper)
        {
            _service = serviceProvider.GetService<IGarmentPurchasingExpeditionService>();
            _identityService = serviceProvider.GetService<IdentityService>();
            _mapper = mapper;
        }

        private void VerifyUser()
        {
            _identityService.Username = User.Claims.ToArray().SingleOrDefault(p => p.Type.Equals("username")).Value;
            _identityService.Token = Request.Headers["Authorization"].FirstOrDefault().Replace("Bearer ", "");
            _identityService.TimezoneOffset = Convert.ToInt32(Request.Headers["x-timezone-offset"]);
        }

        [HttpGet("internal-notes")]
        public IActionResult GetGarmentInternalNotes([FromQuery] string keyword, [FromQuery] GarmentInternalNoteFilterDto filter)
        {
            try
            {
                var result = _service.GetGarmentInternalNotes(keyword,filter);
                return Ok(new
                {
                    apiVersion = ApiVersion,
                    statusCode = General.OK_STATUS_CODE,
                    message = General.OK_MESSAGE,
                    data = result
                });
            }
            catch (Exception e)
            {
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, e.Message + " " + e.StackTrace);
            }
        }

        [HttpGet("disposition-notes")]
        public IActionResult GetGarmentDispositionNotes([FromQuery] string keyword, [FromQuery] PurchasingGarmentExpeditionPosition position)
        {
            try
            {
                var result = _service.GetGarmentDispositionNotes(keyword, position);
                return Ok(new
                {
                    apiVersion = ApiVersion,
                    statusCode = General.OK_STATUS_CODE,
                    message = General.OK_MESSAGE,
                    data = result
                });
            }
            catch (Exception e)
            {
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, e.Message + " " + e.StackTrace);
            }
        }

        [HttpGet("internal-notes-details")]
        public IActionResult GetGarmentInternalNoteDetails([FromQuery] string keyword, [FromQuery] GarmentInternalNoteFilterDto filter)
        {
            try
            {
                var result = _service.GetGarmentInternNotesDetails(keyword, filter);
                var viewModel = _mapper.Map<List<GarmentInternalNoteDetailsDto>>(result);
                return Ok(new
                {
                    apiVersion = ApiVersion,
                    statusCode = General.OK_STATUS_CODE,
                    message = General.OK_MESSAGE,
                    data = result
                });
            }
            catch (Exception e)
            {
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, e.Message + " " + e.StackTrace);
            }
        }

        [HttpPut("internal-notes-update-paid-pph")]
        public IActionResult UpdateGarmentInternalNoteIsPaidPph([FromBody] List<GarmentInternNoteUpdateIsPphPaidDto> listModel )
        {
            try
            {
                _service.UpdateInternNotesIsPphPaid(listModel);
                //var viewModel = _mapper.Map<List<GarmentInternalNoteDetailsDto>>(result);
                return Ok(new
                {
                    apiVersion = ApiVersion,
                    statusCode = General.OK_STATUS_CODE,
                    message = General.OK_MESSAGE
                });
            }
            catch (Exception e)
            {
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, e.Message + " " + e.StackTrace);
            }
        }

        [HttpPut("internal-notes/position")]
        public IActionResult UpdateGarmentInternalNotePosition([FromBody] UpdatePositionFormDto form)
        {
            try
            {
                VerifyUser();

                var result = _service.UpdateInternNotePosition(form);
                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, e.Message + " " + e.StackTrace);
            }
        }

        [HttpPut("disposition-notes/position")]
        public IActionResult UpdateGarmentDispositionNotePosition([FromBody] UpdatePositionFormDto form)
        {
            try
            {
                VerifyUser();

                var result = _service.UpdateDispositionNotePosition(form);
                return NoContent();
            }
            catch (Exception e)
            {
                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, e.Message + " " + e.StackTrace);
            }
        }
    }
}
