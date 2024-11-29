using CMCS.Data;
using CMCS.Models;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

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
            // Validate Model State
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

            // Validate new fields
            if (string.IsNullOrEmpty(claim.LecturerEmail) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(claim.LecturerEmail))
            {
                ModelState.AddModelError("LecturerEmail", "A valid email address is required.");
                return View(claim);
            }

            if (string.IsNullOrEmpty(claim.LecturerPhoneNumber))
            {
                ModelState.AddModelError("LecturerPhoneNumber", "Phone number is required.");
                return View(claim);
            }

            if (string.IsNullOrEmpty(claim.Department))
            {
                ModelState.AddModelError("Department", "Department is required.");
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

            // Save the claim to the database
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

        // HR - Dashboard for Report Generation
        [HttpGet]
        public IActionResult HRDashboard()
        {
            // Fetch approved claims to show a summary or optional preview (if needed)
            var approvedClaims = _context.Claims.Where(c => c.Status == "Approved").ToList();

            // Pass approved claims to the view (optional, if you want a preview on the dashboard)
            return View(approvedClaims);
        }

        // HR - Generate Reports
        [HttpPost]
        public IActionResult GenerateReport()
        {
            // Fetch approved claims
            var approvedClaims = _context.Claims.Where(c => c.Status == "Approved").ToList();

            // Create a new Excel workbook using ClosedXML
            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
                // Add a worksheet
                var worksheet = workbook.Worksheets.Add("Approved Claims Report");

                // Add headers
                worksheet.Cell(1, 1).Value = "Lecturer Name";
                worksheet.Cell(1, 2).Value = "Hours Worked";
                worksheet.Cell(1, 3).Value = "Hourly Rate";
                worksheet.Cell(1, 4).Value = "Total Payment";

                // Add data rows
                for (int i = 0; i < approvedClaims.Count; i++)
                {
                    var claim = approvedClaims[i];
                    worksheet.Cell(i + 2, 1).Value = claim.LecturerName;
                    worksheet.Cell(i + 2, 2).Value = claim.HoursWorked;
                    worksheet.Cell(i + 2, 3).Value = claim.HourlyRate;
                    worksheet.Cell(i + 2, 4).Value = claim.HoursWorked * claim.HourlyRate;
                }

                // Format the header row
                var headerRow = worksheet.Range("A1:D1");
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                // Adjust column widths
                worksheet.Columns().AdjustToContents();

                // Return the Excel file as a downloadable response
                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Seek(0, System.IO.SeekOrigin.Begin);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ApprovedClaimsReport.xlsx");
                }
            }
        }
    }
}