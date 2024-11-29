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
            // Model state validation
            if (!ModelState.IsValid)
            {
                return View(claim); // Return with validation errors
            }

            // Business rule validation: Ensure hours worked and hourly rate are valid
            if (claim.HoursWorked <= 0 || claim.HourlyRate <= 0)
            {
                ModelState.AddModelError("", "Hours Worked and Hourly Rate must be greater than zero.");
                return View(claim);
            }

            // Ensure uploaded file meets requirements
            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                ModelState.AddModelError("uploadedFile", "Supporting document is required.");
                return View(claim);
            }

            // Validate file type and size
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
                // Create uploads directory if it doesn't exist
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Save the file
                var filePath = Path.Combine(uploadsDir, uploadedFile.FileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadedFile.CopyToAsync(stream);
                }

                // Save the document reference in the database
                var document = new UploadedDocument
                {
                    FileName = uploadedFile.FileName,
                    FilePath = "/uploads/" + uploadedFile.FileName
                };
                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                // Assign the document ID to the claim
                claim.DocumentId = document.Id;
            }
            catch (Exception ex)
            {
                // Handle exceptions during file upload
                ModelState.AddModelError("", $"An error occurred while uploading the document: {ex.Message}");
                return View(claim);
            }

            // Set default claim status
            claim.Status = "Pending";

            // Add the claim to the database
            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            // Success message
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

        // Coordinator - Auto-Approve Eligible Claims (POST)
        [HttpPost]
        public async Task<IActionResult> AutoApproveClaims()
        {
            var claimsToApprove = _context.Claims
                .Where(c => c.Status == "Pending" && c.HoursWorked > 10 && c.HourlyRate > 50)
                .ToList();

            foreach (var claim in claimsToApprove)
            {
                claim.Status = "Approved";
                claim.Notes = "Automatically approved based on criteria.";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Eligible claims have been auto-approved.";
            return RedirectToAction(nameof(ManageClaims));
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

        // HR - Generate Reports
        public IActionResult GenerateReport()
        {
            var approvedClaims = _context.Claims.Where(c => c.Status == "Approved").ToList();

            // Logic to generate report (e.g., Excel or PDF)...
            var reportData = approvedClaims.Select(c => new
            {
                c.LecturerName,
                c.HoursWorked,
                c.HourlyRate,
                TotalPayment = c.HoursWorked * c.HourlyRate
            });

            // Use a library like EPPlus to create Excel files or a PDF library for PDFs.
            // This example returns the report data in JSON for simplicity.
            return Ok(reportData);
        }
    }
}