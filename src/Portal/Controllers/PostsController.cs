using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portal.Data;
using Portal.Models;
using Portal.ViewModels;

namespace Portal.Controllers;

[Authorize]
public class PostsController(PortalContext context, UserManager<PortalUser> userManager) : Controller
{
    public async Task<IActionResult> Index(string? search, PostCategory? category)
    {
        var query = context.Posts.Include(post => post.Author).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(post => EF.Functions.Like(post.Title, pattern) || EF.Functions.Like(post.Body, pattern));
        }

        if (category is not null)
        {
            query = query.Where(post => post.Category == category);
        }

        var posts = await query.OrderByDescending(post => post.CreatedAt).ToListAsync();

        return View(new PostsIndexViewModel
        {
            Posts = posts,
            Search = search,
            Category = category
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var post = await context.Posts
            .Include(post => post.Author)
            .FirstOrDefaultAsync(post => post.Id == id);

        if (post is null)
        {
            return NotFound();
        }

        return View(post);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PostFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var now = DateTime.UtcNow;
        var post = new Post
        {
            Title = model.Title,
            Body = model.Body,
            Category = model.Category,
            AuthorId = userManager.GetUserId(User)!,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Posts.Add(post);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = post.Id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var post = await context.Posts.FirstOrDefaultAsync(post => post.Id == id);

        if (post is null || post.AuthorId != userManager.GetUserId(User))
        {
            return NotFound();
        }

        var model = new PostFormViewModel
        {
            Title = post.Title,
            Body = post.Body,
            Category = post.Category
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PostFormViewModel model)
    {
        var post = await context.Posts.FirstOrDefaultAsync(post => post.Id == id);

        if (post is null || post.AuthorId != userManager.GetUserId(User))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        post.Title = model.Title;
        post.Body = model.Body;
        post.Category = model.Category;
        post.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = post.Id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var post = await context.Posts
            .Include(post => post.Author)
            .FirstOrDefaultAsync(post => post.Id == id);

        if (post is null || post.AuthorId != userManager.GetUserId(User))
        {
            return NotFound();
        }

        return View(post);
    }

    [HttpPost]
    [ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var post = await context.Posts.FirstOrDefaultAsync(post => post.Id == id);

        if (post is null || post.AuthorId != userManager.GetUserId(User))
        {
            return NotFound();
        }

        context.Posts.Remove(post);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
