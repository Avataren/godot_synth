shader_type canvas_item;

#define BUFFER_SIZE 512
uniform float wave_data[BUFFER_SIZE];
uniform float line_thickness = 0.02; // Thickness of the line (in normalized space)
uniform float aspect_ratio = 1.0; // Aspect ratio of the texture (width / height)

// Improved SDF calculation for line segments
float sdSegment(vec2 p, vec2 a, vec2 b) {
    vec2 pa = p - a;
    vec2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h);
}

void fragment() {
    vec2 uv = UV;
    uv.x *= aspect_ratio; // Correct UV mapping for non-square textures
    float min_distance = 1.0; // Large initial distance

    // Iterate over waveform segments
    for (int i = 0; i < BUFFER_SIZE - 1; i++) {
        vec2 p1 = vec2(float(i) / float(BUFFER_SIZE - 1) * aspect_ratio, 0.5 + 0.5 * wave_data[i]);
        vec2 p2 = vec2(float(i + 1) / float(BUFFER_SIZE - 1) * aspect_ratio, 0.5 + 0.5 * wave_data[i + 1]);
        float dist_to_segment = sdSegment(uv, p1, p2);
        min_distance = min(min_distance, dist_to_segment);
    }

    // Signed distance field adjusted for aspect ratio
    float sdf = min_distance - line_thickness;

    // Intensity based on SDF
    float intensity = 1.0 - smoothstep(0.0, line_thickness * 2.0, sdf);

    vec3 color = vec3(0.6, 0.8, 0.9); // Adjusted for visual consistency
    COLOR = vec4(color * intensity, intensity);
}
