shader_type canvas_item;
#define BUFFER_SIZE 512
uniform bool show_debug = true;
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

float get_data(int index) {
    if (show_debug) {
        float attack_time = 0.2;
        float decay_time = 0.2;
        float sustain_level = 0.5;
        float release_time = 0.2;
        float t = float(index) / float(BUFFER_SIZE - 1);
        if (t < attack_time) {
            return t / attack_time;
        } else if (t < attack_time + decay_time) {
            return 1.0 - ((t - attack_time) / decay_time) * (1.0 - sustain_level);
        } else if (t < 1.0 - release_time) {
            return sustain_level;
        } else {
            return sustain_level * (1.0 - (t - (1.0 - release_time)) / release_time);
        }
    } else {
        return wave_data[index];
    }
}

void fragment() {
    vec2 uv = UV;
    uv.x *= aspect_ratio; // Correct UV mapping for non-square textures

    float min_distance = 1.0; // Large initial distance
    float max_wave_y = 0.0; // Track highest point of the waveform for gradient

    // Iterate over waveform segments
    for (int i = 0; i < BUFFER_SIZE - 1; i++) {
		float y1 = 1.0;
		if (i > 0)
         y1 = 1.0 - get_data(i);
        float y2 = 1.0 - get_data(i + 1);
		if (i == BUFFER_SIZE - 2)
		{
			y2 = 1.0;
		}
        vec2 p1 = vec2(float(i) / float(BUFFER_SIZE - 1) * aspect_ratio, y1);
        vec2 p2 = vec2(float(i + 1) / float(BUFFER_SIZE - 1) * aspect_ratio, y2);
        float dist_to_segment = sdSegment(uv, p1, p2);
        min_distance = min(min_distance, dist_to_segment);
        max_wave_y = max(max_wave_y, max(y1, y2)); // Get the maximum y position of the waveform
    }

    // Add vertical lines to indicate whole seconds
    float seconds_interval = aspect_ratio / total_time;
    float line_intensity = 0.0;
    for (float t = 0.0; t <= total_time; t += 1.0) {
        float x_pos = t * seconds_interval;
        float dist_to_line = abs(uv.x - x_pos);
        float adjusted_thickness = vertical_line_thickness * aspect_ratio;
        line_intensity += smoothstep(adjusted_thickness, 0.0, dist_to_line);
    }

    // Signed distance field adjusted for aspect ratio
    float sdf = min_distance - line_thickness;
    // Intensity based on SDF
    float intensity = smoothstep(line_thickness * 2.0, 0.0, sdf);

    // Compute gradient alpha below the waveform
    float gradient_alpha = (uv.y > max_wave_y) ? 1.0 : 0.0;

    // Combine waveform intensity with vertical line intensity
    vec3 color = mix(vertical_line_color * line_intensity, waveform_color * intensity, step(line_intensity, intensity));
    float alpha = max(intensity, line_intensity);

    // Apply the gradient

	float gradient = pow(1.0-sdf, 12.5);
	vec3 gradientColor = vec3(0.1,0.4, 0.5);

    COLOR = vec4(color, alpha) * (1.0 - gradient_alpha) + vec4(0.0, 0.0, 0.0, gradient_alpha) + vec4(gradientColor, gradient*0.5);
}
