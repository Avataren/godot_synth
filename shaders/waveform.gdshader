shader_type canvas_item;

// Define the buffer size according to the data you are passing
const int BUFFER_SIZE = 1024;
uniform float wave_data[BUFFER_SIZE];

void fragment() {
    // Normalize the position of the pixel along the width of the TextureRect
    int sample_index = int(UV.x * 0.1 * float(BUFFER_SIZE - 1));
    // Get the amplitude from the waveform data
    float amplitude = wave_data[sample_index];
    // Map amplitude to visual height (centered and adjusted for a 0-1 range)
    float visual_y = 0.5 + 0.5 * amplitude;  // assuming amplitude is in range [-1, 1]
    // Color adjustment using smoothstep for a soft edge
    float intensity = 1.0 - smoothstep(0.0, 0.025, abs(visual_y - UV.y));
    vec3 color = vec3(0.6, 0.8, 0.9);
    // Apply a color based on intensity
    COLOR = vec4(color * intensity, intensity);  // Red color with varying alpha for visibility
}
