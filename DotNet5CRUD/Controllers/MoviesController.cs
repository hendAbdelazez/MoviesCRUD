using DotNet5CRUD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Movies.ViewModels;
using NToastNotify;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotNet5CRUD.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toastNotification;
        private List<string> _allowedExcetntion = new List<string> { ".jpg", ".png" };
        private long _maxAllowedPosterSize = 1048576;

    
        public MoviesController(ApplicationDbContext context, IToastNotification toastNotification) 
        {
            _context = context;
            _toastNotification = toastNotification;
        }
        public async Task<IActionResult> Index()
        {
            var Movies = await _context.Movies.OrderByDescending(m => m.Rate).ToListAsync();
            return View(Movies);
        }
        public async Task<IActionResult> Create()
        {
            var ViewModel = new MovieFormViewModel
            {
                Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync()
            };
            return View("MovieForm", ViewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
            }

            var files = Request.Form.Files;
            if (!files.Any())
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "please select Movie Poster!");
                return View("MovieForm", model);
            }
            var Poster = files.FirstOrDefault();

            if (!_allowedExcetntion.Contains(Path.GetExtension(Poster.FileName).ToLower()))
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Only.png,.jpg images are allowed!");
                return View("MovieForm", model);
            }
            if (Poster.Length > _maxAllowedPosterSize)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Poster cannot be more than 1 Mb");
                return View(model);
            }
            using var dataStream = new MemoryStream();
            await Poster.CopyToAsync(dataStream);
            var movies = new Movie
            {
                Title = model.Title,
                GenreId = model.GenreId,
                Year = model.Year,
                Rate = model.Rate,
                StoryLine = model.StoryLine,
                Poster = dataStream.ToArray(),
            };
            _context.Movies.Add(movies);
            _context.SaveChanges();
            _toastNotification.AddSuccessToastMessage("MovieCreatedSuccessful");
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return BadRequest();

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound();
            var ViewModel = new MovieFormViewModel
            {

                Id = movie.id,
                Title = movie.Title,
                Rate = movie.Rate,
                Year = movie.Year,
                StoryLine = movie.StoryLine,
                Poster = movie.Poster,
                Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync()
            };
            return View("MovieForm", ViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MovieFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
            }
            var movie = await _context.Movies.FindAsync(model.Id);
            if (movie == null)
                return NotFound();

            var files = Request.Form.Files;
            if (files.Any())
            {
                var Poster = files.FirstOrDefault();
                using var datastream = new MemoryStream();
                await Poster.CopyToAsync(datastream);

                model.Poster = datastream.ToArray();
                if (!_allowedExcetntion.Contains(Path.GetExtension(Poster.FileName).ToLower()))
                {
                    model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Only.png,.jpg images are allowed!");
                    return View("MovieForm", model);
                }
                if (Poster.Length > _maxAllowedPosterSize)
                {
                    model.Genres = await _context.Genres.OrderBy(m => m.Name).ToListAsync();
                    ModelState.AddModelError("Poster", "Poster cannot be more than 1 Mb");
                    return View(model);
                }
                movie.Poster = model.Poster;

            }

            movie.Title = model.Title;
            movie.GenreId = model.GenreId;
            movie.Year = model.Year;
            movie.Rate = model.Rate;
            movie.StoryLine = model.StoryLine;
            _context.SaveChanges();
            _toastNotification.AddSuccessToastMessage("MovieiEditedSuccessful");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int?id)
        {
            if (id == null)
                return BadRequest();
            var movie = await _context.Movies.Include(m => m.Genre).SingleOrDefaultAsync(m => m.id == id);
            if (movie == null)
                return NotFound();
            return View(movie);
        }

        public async Task<IActionResult> Delete (int? id)
        {
            if (id == null)
                return BadRequest();
            var movie = await _context.Movies.FindAsync(id);
            if (movie == null)
                return NotFound();
            _context.Movies.Remove(movie);
            _context.SaveChanges();
            return Ok();
        }

    }


}
