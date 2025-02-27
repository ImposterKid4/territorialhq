﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TerritorialHQ.Models;
using TerritorialHQ.Services;
using TerritorialHQ_Library.Entities;

namespace TerritorialHQ.Areas.Administration.Pages.Clans
{
    [Authorize(Roles = "Administrator")]
    public class DeleteModel : PageModel
    {
        private readonly ApisService _service;
        private readonly IWebHostEnvironment _env;

        public DeleteModel(ApisService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        [BindProperty]
        public Clan? Clan { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null)
                return NotFound();

            Clan = await _service.FindAsync<Clan>("Clan", id);

            if (Clan == null)
                return NotFound();

            return Page();
        }

        public async Task<IActionResult>
            OnPostAsync(string id)
        {
            if (id == null)
                return NotFound();

            var item = await _service.FindAsync<Clan>("Clan", id);
            if (item == null)
                return NotFound();

            if (item.LogoFile != null)
            {
                var oldFilePath = Path.Combine(_env.WebRootPath + "/Data/Uploads/System/", item.LogoFile);
                if (System.IO.File.Exists(oldFilePath))
                    System.IO.File.Delete(oldFilePath);
            }
            if (item.BannerFile != null)
            {
                var oldFilePath = Path.Combine(_env.WebRootPath + "/Data/Uploads/System/", item.BannerFile);
                if (System.IO.File.Exists(oldFilePath))
                    System.IO.File.Delete(oldFilePath);
            }

            if (!(await _service.Remove<Clan>("Clan", id)))
                throw new Exception("Error while saving data set.");

            return RedirectToPage("./Index");
        }
    }
}
