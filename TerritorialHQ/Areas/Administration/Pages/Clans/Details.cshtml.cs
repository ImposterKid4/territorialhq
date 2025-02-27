﻿using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TerritorialHQ.Models;
using TerritorialHQ.Services;
using TerritorialHQ_Library.Entities;

namespace TerritorialHQ.Areas.Administration.Pages.Clans
{
    [Authorize(Roles = "Administrator, Staff")]
    public class DetailsModel : PageModel
    {
        private readonly ApisService _service;
        private readonly AppUserService _userService;
        private readonly DiscordBotService _discordBotService;

        public DetailsModel(ApisService service,  DiscordBotService discordBotService, AppUserService userService)
        {
            _service = service;
            _discordBotService = discordBotService;
            _userService = userService;
        }

        public Clan? Clan { get; set; }
        public List<ClanUserRelation>? UserRelations { get; set; }


        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (id == null)
                return NotFound();

            Clan = await _service.FindAsync<Clan>("Clan", id);
            if (Clan == null)
                return NotFound();

            UserRelations = await _service.GetAllAsync<ClanUserRelation>("ClanUserRelation") ?? new List<ClanUserRelation>();

            await FillStaffUserSelect();

            return Page();
        }

        public async Task<IActionResult> OnPostAddUser(string id, string userId)
        {
            Clan = await _service.FindAsync<Clan>("Clan", id);
            if (Clan == null)
                return NotFound(); 
            
            UserRelations = await _service.GetAllAsync<ClanUserRelation>("ClanUserRelation") ?? new List<ClanUserRelation>();

            var user = await _service.FindAsync<AppUser>("AppUser", userId);
            if (user != null)
            {
                if (!UserRelations.Any(r => r.AppUserId == user.Id))
                {
                    var relation = new ClanUserRelation() { ClanId = Clan.Id, AppUserId = user.Id };

                    if (!(await _service.Add<ClanUserRelation>("ClanUserRelation", relation)))
                        throw new Exception("Error while saving relation data set.");
                }
            }

            return RedirectToPage("./Details", new { id = Clan.Id });
        }


        public async Task<IActionResult> OnPostRemoveUser(string id, string userId)
        {
            Clan = await _service.FindAsync<Clan>("Clan", id);
            if (Clan == null)
                return NotFound();

            var relationToRemove = await _service.FindAsync<ClanUserRelation>("ClanUserRelation", userId);
            if (relationToRemove != null)
                if(!(await _service.Remove<ClanUserRelation>("ClanUserRelation", relationToRemove.Id)))
                        throw new Exception("Error while saving relation data set.");

            UserRelations = await _service.GetAllAsync<ClanUserRelation>("ClanUserRelation") ?? new List<ClanUserRelation>();
            await FillStaffUserSelect();

            return Page();
        }

        public async Task<IActionResult> OnPostMarkForReview(string? id)
        {
            Clan = await _service.FindAsync<Clan>("Clan", id);
            if (Clan == null)
                return NotFound();

            Clan.InReview = true;

            if (!(await _service.Update<Clan>("Clan", Clan)))
                throw new Exception("Error while saving data set.");

            UserRelations = await _service.GetAllAsync<ClanUserRelation>("ClanUserRelation") ?? new List<ClanUserRelation>();
            await FillStaffUserSelect();

            await _discordBotService.SendReviewNotificationAsync(User.Identity?.Name, id);

            return RedirectToPage("./Details", new { id });
        }

        public async Task<IActionResult> OnPostPublish(string id)
        {
            Clan = await _service.FindAsync<Clan>("Clan", id);
            if (Clan == null)
                return NotFound();

            if (User.IsInRole("Administrator"))
            {
                Clan.InReview = false;
                Clan.IsPublished = true;

                if (!(await _service.Update<Clan>("Clan", Clan)))
                    throw new Exception("Error while saving data set.");
            }

            UserRelations = await _service.GetAllAsync<ClanUserRelation>("ClanUserRelation") ?? new List<ClanUserRelation>();
            await FillStaffUserSelect();

            return Page();
        }

        private async Task FillStaffUserSelect()
        {
            if (User.IsInRole("Administrator"))
            {
                var assignedStaffUsersIds = UserRelations?.Select(s => s.AppUserId).ToList() ?? new List<string?>();
                var staffUsers = await _userService.GetUsersInRoleAsync(AppUserRole.Staff);

                staffUsers!.RemoveAll(u => assignedStaffUsersIds.Contains(u.Id));

                ViewData["UserId"] = new SelectList(staffUsers, "Id", "UserName");
            }
        }
    }
}
