#include <iostream>
#include <cmath>
#include <string>

// A simple high-performance physics simulation for STEMd
int main(int argc, char* argv[]) {
    if (argc < 4) {
        std::cerr << "Usage: Simulation.exe <initial_velocity> <launch_angle_degrees> <time_step>" << std::endl;
        return 1;
    }

    try {
        double v0 = std::stod(argv[1]);
        double angle = std::stod(argv[2]);
        double dt = std::stod(argv[3]);

        const double g = 9.81;
        double rad = angle * 3.14159265 / 180.0;
        
        double vx = v0 * cos(rad);
        double vy = v0 * sin(rad);

        double t = 0.0;
        double x = 0.0;
        double y = 0.0;

        std::cout << "["; // Start JSON array
        
        bool first = true;
        while (y >= 0.0) {
            if (!first) std::cout << ",";
            
            std::cout << "{\"t\":" << t << ",\"x\":" << x << ",\"y\":" << y << "}";
            
            t += dt;
            x = vx * t;
            y = (vy * t) - (0.5 * g * t * t);
            first = false;
        }
        
        std::cout << "]" << std::endl; // End JSON array
        
        return 0;
    } catch (const std::exception& e) {
        std::cerr << "Error during simulation: " << e.what() << std::endl;
        return 1;
    }
}
