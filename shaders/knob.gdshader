shader_type canvas_item;

uniform float progress : hint_range(0.0, 1.0) = 0.0;  // 75% progress
uniform float start_angle : hint_range(0.0, 360.0) = 270.0;  // Start at the bottom (270 degrees)
uniform float arc_span : hint_range(0.0, 360.0) = 270.0;  // Total span of the arc
uniform float thickness : hint_range(0.0, 0.5) = 0.1;  // Thickness as fraction of radius
uniform vec4 color_bg : source_color = vec4(0.2, 0.2, 0.2, 1.0);  // Background arc color
uniform vec4 color_fg : source_color = vec4(0.67, 0.74, 0.86, 1.0);  // Progress arc color
uniform vec4 color_pointer : source_color = vec4(1.0, 0.5, 0.0, 1.0);  // Pointer color
uniform vec4 color_cap : source_color = vec4(0.1, 0.1, 0.1, 1.0);  // Cap color
uniform float antialiasing : hint_range(0.0, 1.0) = 0.01;  // Anti-aliasing strength
uniform float pointer_thickness : hint_range(0.0, 0.1) = 0.01;  // Thickness of the pointer line
uniform float taper_amount : hint_range(0.0, 1.0) = 0.5;  // Amount of taper towards the center
uniform float cap_radius : hint_range(0.0, 0.2) = 0.05;  // Radius of the center cap

void fragment() {
    vec2 uv = UV;
    vec2 center = vec2(0.5, 0.5);
    vec2 dir = uv - center;
    dir.y = -dir.y;

    float dist = length(dir);
    float radius = 0.4;
    float angle = degrees(atan(dir.y, dir.x));
    if (angle < 0.0) {
        angle += 360.0;
    }

    float progress_angle = mod(start_angle - (progress * arc_span), 360.0);
    float end_angle = mod(start_angle - arc_span, 360.0);

    vec4 arc_color = vec4(0.0);
    bool within_arc = false;

    if (start_angle >= end_angle) {
        within_arc = (angle <= start_angle && angle >= end_angle);
    } else {
        within_arc = (angle >= end_angle || angle <= start_angle);
    }

    if (within_arc) {
        if ((start_angle >= progress_angle && angle <= start_angle && angle >= progress_angle) ||
            (start_angle < progress_angle && (angle >= progress_angle || angle <= start_angle))) {
            arc_color = color_fg;
        } else {
            arc_color = color_bg;
        }

        float edge = smoothstep(radius - thickness - antialiasing, radius - thickness + antialiasing, dist) -
                     smoothstep(radius - antialiasing, radius + antialiasing, dist);

        arc_color = mix(color_bg, arc_color, edge);
        arc_color.a *= edge;
    } else {
        arc_color = vec4(0.0);
    }

    vec2 pointer_dir = vec2(cos(radians(progress_angle)), sin(radians(progress_angle)));

    float pointer_dist = dot(dir, pointer_dir);
    float taper_factor = 1.0 - (dist / radius) * taper_amount;
	taper_factor = pow(taper_factor, 1.5);
    float tapered_thickness = pointer_thickness * taper_factor;

    float perpendicular_dist = length(dir - pointer_dir * pointer_dist);

    if (pointer_dist > 0.0 && perpendicular_dist < tapered_thickness && dist <= radius) {
        float pointer_edge = smoothstep(tapered_thickness + antialiasing, tapered_thickness - antialiasing, perpendicular_dist);
        vec4 pointer_color = mix(arc_color, color_pointer, pointer_edge);
        arc_color = pointer_color;
    }

    // Draw the center cap
    if (dist < cap_radius) {
        arc_color = color_cap;
    }

    COLOR = arc_color;
}
