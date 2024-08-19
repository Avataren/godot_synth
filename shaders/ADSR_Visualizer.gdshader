shader_type canvas_item;
#define DEBUG
#define BUFFER_SIZE 512
uniform float wave_data[BUFFER_SIZE];
uniform float line_thickness = 0.002; // Thickness of the waveform line (in normalized space)
uniform float vertical_line_thickness = 0.003; // Thickness of the vertical lines (in normalized space)
uniform float aspect_ratio = 2.0; // Aspect ratio of the texture (width / height)
uniform float total_time = 4.0; // Total duration of the waveform in seconds
uniform vec3 waveform_color = vec3(0.6, 0.8, 0.9); // Color of the waveform line
uniform vec3 vertical_line_color = vec3(0.8, 0.8, 0.8); // Color of the vertical lines

// Simplified SDF calculation for line segments
float sdSegment(vec2 p, vec2 a, vec2 b) {
    vec2 ba = b - a;
    vec2 pa = p - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

float get_data(int index)
{
    #ifdef DEBUG
        // Simple ADSR envelope with normalized indices (0 to 1)
        float attack_time = 0.2;
        float decay_time = 0.2;
        float sustain_level = 0.5; // Sustain level (normalized amplitude)
        float release_time = 0.2;

        float t = float(index) / float(BUFFER_SIZE - 1);

        if (t < attack_time) {
            return t / attack_time; // Linear attack: ramp up from 0 to 1
        } else if (t < attack_time + decay_time) {
            return 1.0 - ((t - attack_time) / decay_time) * (1.0 - sustain_level); // Linear decay: ramp down to sustain level
        } else if (t < 1.0 - release_time) {
            return sustain_level; // Sustain phase: hold at sustain level
        } else {
            return sustain_level * (1.0 - (t - (1.0 - release_time)) / release_time); // Linear release: ramp down to 0
        }
    #else
        return wave_data[index];
    #endif
}

void fragment() {
    vec2 uv = UV;
    uv.x *= aspect_ratio; // Correct UV mapping for non-square textures

    float min_distance = 1.0; // Large initial distance

    // Iterate over waveform segments
    for (int i = 0; i < BUFFER_SIZE - 1; i++) {
        // Convert the data from 0-1 range to -1 to 1 range and flip vertically
        float y1 = 1.0 - get_data(i);          // Invert the Y axis (0 at top, 1 at bottom)
        float y2 = 1.0 - get_data(i + 1);      // Invert the Y axis (0 at top, 1 at bottom)

        // Create points for the current segment
        vec2 p1 = vec2(float(i) / float(BUFFER_SIZE - 1) * aspect_ratio, y1);
        vec2 p2 = vec2(float(i + 1) / float(BUFFER_SIZE - 1) * aspect_ratio, y2);
        
        // Calculate distance to segment
        float dist_to_segment = sdSegment(uv, p1, p2);
        min_distance = min(min_distance, dist_to_segment);
    }

    // Add vertical lines to indicate whole seconds
    float seconds_interval = aspect_ratio / total_time;
    float line_intensity = 0.0;

    for (float t = 0.0; t <= total_time; t += 1.0) {
        float x_pos = t * seconds_interval;
        float dist_to_line = abs(uv.x - x_pos);
        // Use aspect ratio to ensure vertical line thickness is consistent across different aspect ratios
        float adjusted_thickness = vertical_line_thickness * aspect_ratio;
        line_intensity += smoothstep(adjusted_thickness, 0.0, dist_to_line);
    }

    // Signed distance field adjusted for aspect ratio
    float sdf = min_distance - line_thickness;

    // Intensity based on SDF
    float intensity = smoothstep(line_thickness * 2.0, 0.0, sdf);

    // Combine waveform intensity with vertical line intensity
    vec3 color = mix(vertical_line_color * line_intensity, waveform_color * intensity, step(line_intensity, intensity));

    COLOR = vec4(color, max(intensity, line_intensity));
}
