using CMCS.Data;
using CMCS.Models;
using Microsoft.AspNetCore.Mvc;

namespace CMCS.Controllers
{
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClaimsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Homepage
        public IActionResult Index()
        {
            return View();
        }

        // Lecturer - Submit a Claim (GET)
        [HttpGet]
        public IActionResult SubmitClaim()
        {
            return View();
        }

        // Lecturer - Submit a Claim (POST)
        [HttpPost]
        public async Task<IActionResult> SubmitClaim(MonthlyClaim claim, IFormFile uploadedFile)
        {
            if (!ModelState.IsValid)
            {
                return View(claim); // Return with validation errors
            }

            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                ModelState.AddModelError("uploadedFile", "Supporting document is required.");
                return View(claim);
            }

            var allowedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
            var fileExtension = Path.GetExtension(uploadedFile.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("uploadedFile", "Invalid file type. Only PDF, DOCX, and XLSX are allowed.");
                return View(claim);
            }

            if (uploadedFile.Length > 2 * 1024 * 1024) // 2MB size limit
            {
                ModelState.AddModelError("uploadedFile", "File size cannot exceed 2MB.");
                return View(claim);
            }

            try
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }
                var filePath = Path.Combine(uploadsDir, uploadedFile.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(stream);
                }

                var document = new UploadedDocument
                {
                    FileName = uploadedFile.FileName,
                    FilePath = "/uploads/" + uploadedFile.FileName
                };
                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                claim.DocumentId = document.Id;
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An error occurred while uploading the document. Please try again.");
                return View(claim);
            }

            claim.Status = "Pending";
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Claim submitted successfully!";
            return RedirectToAction(nameof(ViewClaims));
        }

        // Lecturer - View Claims
        public IActionResult ViewClaims()
        {
            var claims = _context.Claims
                .Select(c => new MonthlyClaim
                {
                    Id = c.Id,
                    LecturerName = c.LecturerName,
                    HoursWorked = c.HoursWorked,
                    HourlyRate = c.HourlyRate,
                    Status = c.Status,
                    Notes = c.Notes,
                    Document = c.Document
                })
                .ToList();

            return View(claims);
        }

        // Coordinator - Manage Claims (GET)
        public IActionResult ManageClaims()
        {
            var pendingClaims = _context.Claims
                .Where(c => c.Status == "Pending")
                .Select(c => new MonthlyClaim
                {
                    Id = c.Id,
                    LecturerName = c.LecturerName,
                    HoursWorked = c.HoursWorked,
                    HourlyRate = c.HourlyRate,
                    Notes = c.Notes,
                    Status = c.Status,
                    Document = c.Document
                })
                .ToList();

            return View(pendingClaims);
        }

        // Coordinator - Approve or Reject a Claim
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> UpdateClaimStatus(int id, string status, string notes)
        {
            var claim = _context.Claims.FirstOrDefault(c => c.Id == id);
            if (claim != null)
            {
                claim.Status = status;
                claim.Notes = notes; // Save the notes regardless of approval or rejection
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ManageClaims));
        }

        // Coordinator - Approve a Claim (Specific Action)
        [HttpPost]
        public async Task<IActionResult> ApproveClaim(int id)
        {
            var claim = _context.Claims.FirstOrDefault(c => c.Id == id);
            if (claim != null)
            {
                claim.Status = "Approved";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Claim approved successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Claim not found!";
            }

            return RedirectToAction(nameof(ManageClaims));
        }

        // Coordinator - Reject a Claim (Specific Action)
        [HttpPost]
        public async Task<IActionResult> RejectClaim(int id, string rejectionNotes)
        {
            var claim = _context.Claims.FirstOrDefault(c => c.Id == id);
            if (claim != null)
            {
                claim.Status = "Rejected";
                claim.Notes = rejectionNotes;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Claim rejected successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Claim not found!";
            }

            return RedirectToAction(nameof(ManageClaims));
        }
    }
}