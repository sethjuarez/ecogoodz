using EcoGoodz.Data;
using EcoGoodz.Data.Models;
using EcoGoodz.Web.Identity;
using EcoGoodz.Web.Models.StaffTask;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EcoGoodz.Web.Controllers;

[Authorize]
public class StaffTaskController : Controller
{
    private readonly EcoGoodzDbContext _context;

    public StaffTaskController(EcoGoodzDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(bool includeCompleted = false)
    {
        var query = _context.AssignTasks
            .Where(t => t.IsActive && t.Task != null && t.Task.IsActive)
            .Include(t => t.AssignedToNavigation)
            .Include(t => t.Task)
                .ThenInclude(t => t!.CreatedByNavigation)
            .Include(t => t.TaskHeadlineNavigation)
            .AsQueryable();

        if (!includeCompleted)
        {
            query = query.Where(t => !t.IsDone);
        }

        var tasks = await query
            .OrderBy(t => t.Task!.Duedate == null)
            .ThenBy(t => t.Task!.Duedate)
            .ThenByDescending(t => t.Id)
            .Select(t => new StaffTaskListItemViewModel
            {
                Id = t.Id,
                TaskId = t.TaskId ?? 0,
                Description = t.Task!.Description ?? string.Empty,
                DueDate = t.Task.Duedate,
                AssignedToName = t.AssignedToNavigation != null
                    ? t.AssignedToNavigation.FirstName + " " + t.AssignedToNavigation.LastName
                    : null,
                Headline = t.TaskHeadlineNavigation != null ? t.TaskHeadlineNavigation.Headline : null,
                CreatedByName = t.Task.CreatedByNavigation != null
                    ? t.Task.CreatedByNavigation.FirstName + " " + t.Task.CreatedByNavigation.LastName
                    : null,
                IsDone = t.IsDone,
                DoneDate = t.DoneDate,
                IsActive = t.IsActive,
            })
            .ToListAsync();

        return View(tasks);
    }

    public async Task<IActionResult> Create()
    {
        var model = new StaffTaskFormViewModel();
        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StaffTaskFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var headlineId = await ResolveHeadlineAsync(model.AssignedTo!.Value, model.TaskHeadline);
        var task = new TaskItem
        {
            Description = model.Description,
            Duedate = model.DueDate,
            IsActive = model.IsActive,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
            AssignTasks =
            [
                new AssignTask
                {
                    AssignedTo = model.AssignedTo,
                    TaskHeadline = headlineId,
                    IsActive = true,
                    IsRead = model.AssignedTo == User.GetLegacyUserId(),
                },
            ],
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var assignTask = await _context.AssignTasks
            .Include(t => t.Task)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (assignTask?.Task is null)
        {
            return NotFound();
        }

        var model = new StaffTaskFormViewModel
        {
            Id = assignTask.Id,
            TaskId = assignTask.TaskId,
            Description = assignTask.Task.Description ?? string.Empty,
            DueDate = assignTask.Task.Duedate,
            AssignedTo = assignTask.AssignedTo,
            TaskHeadline = assignTask.TaskHeadline,
            IsActive = assignTask.IsActive && assignTask.Task.IsActive,
        };

        await PopulateOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StaffTaskFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateOptionsAsync(model);
            return View(model);
        }

        var assignTask = await _context.AssignTasks
            .Include(t => t.Task)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (assignTask?.Task is null)
        {
            return NotFound();
        }

        assignTask.Task.Description = model.Description;
        assignTask.Task.Duedate = model.DueDate;
        assignTask.AssignedTo = model.AssignedTo;
        assignTask.TaskHeadline = await ResolveHeadlineAsync(model.AssignedTo!.Value, model.TaskHeadline);
        assignTask.IsActive = model.IsActive;
        assignTask.UpdatedOn = DateTime.UtcNow;
        assignTask.UpdatedBy = User.GetLegacyUserId();

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDone(int id, bool done)
    {
        var assignTask = await _context.AssignTasks.FindAsync(id);
        if (assignTask is null)
        {
            return NotFound();
        }

        assignTask.IsDone = done;
        assignTask.DoneDate = done ? DateTime.UtcNow : null;
        assignTask.UpdatedOn = DateTime.UtcNow;
        assignTask.UpdatedBy = User.GetLegacyUserId();
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { includeCompleted = done });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var assignTask = await _context.AssignTasks
            .Include(t => t.Task)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (assignTask is null)
        {
            return NotFound();
        }

        assignTask.IsActive = false;
        assignTask.UpdatedOn = DateTime.UtcNow;
        assignTask.UpdatedBy = User.GetLegacyUserId();

        if (assignTask.TaskId.HasValue
            && !await _context.AssignTasks.AnyAsync(t => t.TaskId == assignTask.TaskId && t.Id != id && t.IsActive))
        {
            assignTask.Task!.IsActive = false;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<int?> ResolveHeadlineAsync(int userId, int? selectedHeadlineId)
    {
        if (selectedHeadlineId.HasValue
            && await _context.TaskHeadlines.AnyAsync(h => h.Id == selectedHeadlineId && h.UserId == userId && h.IsActive == true))
        {
            return selectedHeadlineId;
        }

        var existingHeadlineId = await _context.TaskHeadlines
            .Where(h => h.UserId == userId && h.IsActive == true && h.Headline == "Assigned")
            .Select(h => (int?)h.Id)
            .FirstOrDefaultAsync();

        if (existingHeadlineId.HasValue)
        {
            return existingHeadlineId;
        }

        var headline = new TaskHeadline
        {
            UserId = userId,
            Headline = "Assigned",
            IsActive = true,
            CreateOn = DateTime.UtcNow,
            CreatedBy = User.GetLegacyUserId(),
        };
        _context.TaskHeadlines.Add(headline);
        await _context.SaveChangesAsync();
        return headline.Id;
    }

    private async Task PopulateOptionsAsync(StaffTaskFormViewModel model)
    {
        model.UserOptions = await _context.Users
            .Where(u => u.IsActive == true)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new SelectListItem
            {
                Value = u.Id.ToString(),
                Text = u.FirstName + " " + u.LastName,
            })
            .ToListAsync();

        model.HeadlineOptions = await _context.TaskHeadlines
            .Where(h => h.IsActive == true && (!model.AssignedTo.HasValue || h.UserId == model.AssignedTo))
            .OrderBy(h => h.Headline)
            .Select(h => new SelectListItem { Value = h.Id.ToString(), Text = h.Headline })
            .ToListAsync();
    }
}
