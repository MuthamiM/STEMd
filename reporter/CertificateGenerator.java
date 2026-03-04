import java.io.FileWriter;
import java.io.IOException;
import java.time.LocalDateTime;
import java.time.format.DateTimeFormatter;

public class CertificateGenerator {
    
    public static void main(String[] args) {
        if (args.length < 3) {
            System.err.println("Usage: java CertificateGenerator <username> <score> <outputPath>");
            System.exit(1);
        }

        String username = args[0];
        String score = args[1];
        String outputPath = args[2];
        
        DateTimeFormatter dtf = DateTimeFormatter.ofPattern("yyyy/MM/dd HH:mm:ss");
        LocalDateTime now = LocalDateTime.now();

        String certificateContent = 
            "==========================================================\n" +
            "                 STEMd ACHIEVEMENT CERTIFICATE            \n" +
            "==========================================================\n\n" +
            "This certifies that:\n" +
            "   " + username.toUpperCase() + "\n\n" +
            "Has successfully completed advanced polyglot learning modules.\n" +
            "Total Accumulated Score: " + score + " points\n\n" +
            "Date Issued: " + dtf.format(now) + "\n\n" +
            "Signature:\n" +
            "STEMd AI Automation Engine                                \n" +
            "==========================================================";

        try {
            FileWriter writer = new FileWriter(outputPath + "/" + username + "_certificate.txt");
            writer.write(certificateContent);
            writer.close();
            System.out.println("Certificate successfully generated at: " + outputPath + "/" + username + "_certificate.txt");
        } catch (IOException e) {
            System.err.println("Failed to write certificate: " + e.getMessage());
            System.exit(1);
        }
    }
}
