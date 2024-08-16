shader_type canvas_item;

#define BUFFER_SIZE 64

uniform float curve_data[BUFFER_SIZE]; // Array of 64 floats, values between -1 and 1
uniform vec4 curve_color = vec4(1.0, 1.0, 0.0, 1.0); // Color of the curve
uniform float line_thickness = 0.02; // Thickness of the curve (in UV space)
uniform float height_factor = 1.0; // Factor to adjust the height (0 to 1)

float sdSegment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

void fragment() {
    vec2 uv = UV;

    // Start with a large distance, reduce as we find closer segments
    float min_distance = 1.0;

    // Iterate over waveform segments
    for (int i = 0; i < BUFFER_SIZE - 1; i++) {
        vec2 p1 = vec2(float(i) / float(BUFFER_SIZE - 1), curve_data[i] * height_factor * 0.5 + 0.5);
        vec2 p2 = vec2(float(i + 1) / float(BUFFER_SIZE - 1), curve_data[i + 1] * height_factor * 0.5 + 0.5);

        // Calculate the distance to the current segment
        float dist_to_segment = sdSegment(uv, p1, p2);

        // Keep the minimum distance
        min_distance = min(min_distance, dist_to_segment);
    }

    // Generate the signed distance field
    float sdf = min_distance - line_thickness;

    // Map the SDF to intensity; ensure the line is visible
    float intensity = 1.0 - smoothstep(0.0, line_thickness * 2.0, sdf);

    // Apply the color with the calculated intensity
    COLOR = vec4(curve_color.rgb * intensity, curve_color.a * intensity);
}
