shader_type canvas_item;

// Define the buffer size according to the data you are passing
#define BUFFER_SIZE 1024
uniform float[BUFFER_SIZE] wave_data;

void fragment() {
	float uvx = UV.x;
    // Normalize the position of the pixel along the width of the TextureRect
    float exact_index = uvx * float(BUFFER_SIZE - 1);
    int sample_index = int(exact_index);
    int next_sample_index = min(sample_index + 1, BUFFER_SIZE - 1); // Ensure it does not go out of bounds
    // Calculate the fractional part to use for interpolation
    float fraction = fract(exact_index);

    // Linearly interpolate between the current and the next sample
    float amplitude = mix(wave_data[sample_index], wave_data[next_sample_index], fraction);
    
    // Map amplitude to visual height (centered and adjusted for a 0-1 range)
    float visual_y = 0.5 + 0.5 * amplitude;  // assuming amplitude is in range [-1, 1]

    // Color adjustment using smoothstep for a soft edge
    float intensity = 1.0 - smoothstep(0.0, 0.04, abs(visual_y - UV.y));
    vec3 color = vec3(0.6, 0.8, 0.9);  // Adjust the color as desired

    // Apply a color based on intensity
    COLOR = vec4(color * intensity, intensity); 
}
