using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMCS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the Documents table
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"), // Primary key with auto-increment
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false), // File name column
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false) // File path column
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id); // Set primary key
                });

            // Create the Claims table
            migrationBuilder.CreateTable(
                name: "Claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"), // Primary key with auto-increment
                    LecturerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false), // Lecturer name column
                    LecturerEmail = table.Column<string>(type: "nvarchar(max)", nullable: false), // Lecturer email column
                    LecturerPhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false), // Lecturer phone number column
                    Department = table.Column<string>(type: "nvarchar(max)", nullable: false), // Department column
                    HoursWorked = table.Column<double>(type: "float", nullable: false), // Hours worked column
                    HourlyRate = table.Column<double>(type: "float", nullable: false), // Hourly rate column
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true), // Notes column (optional)
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false), // Status column
                    DocumentId = table.Column<int>(type: "int", nullable: true) // Foreign key to Documents table (optional)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Claims", x => x.Id); // Set primary key
                    table.ForeignKey(
                        name: "FK_Claims_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id"); // Set foreign key constraint
                });

            // Create an index on the DocumentId column in the Claims table
            migrationBuilder.CreateIndex(
                name: "IX_Claims_DocumentId",
                table: "Claims",
                column: "DocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the Claims table
            migrationBuilder.DropTable(
                name: "Claims");

            // Drop the Documents table
            migrationBuilder.DropTable(
                name: "Documents");
        }
    }
}
