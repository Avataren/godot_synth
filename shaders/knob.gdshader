shader_type canvas_item;

uniform float progress : hint_range(0.0, 1.0) = 0.0;  // 75% progress
uniform float start_angle : hint_range(0.0, 360.0) = 270.0;  // Start at the bottom (270 degrees)
uniform float arc_span : hint_range(0.0, 360.0) = 270.0;  // Total span of the arc
uniform float thickness : hint_range(0.0, 0.5) = 0.1;  // Thickness as fraction of radius
uniform vec4 color_bg : source_color = vec4(0.2, 0.2, 0.2, 1.0);  // Background arc color
uniform vec4 color_fg : source_color = vec4(0.67, 0.74, 0.86, 1.0);  // Progress arc color
uniform vec4 color_pointer : source_color = vec4(1.0, 0.5, 0.0, 1.0);  // Pointer color
uniform float antialiasing : hint_range(0.0, 1.0) = 0.01;  // Anti-aliasing strength
uniform float pointer_thickness : hint_range(0.0, 0.05) = 0.01;  // Thickness of the pointer line

void fragment() {
    // Normalized UV coordinates [0, 1]
    vec2 uv = UV;
    
    // Define the center of the UV space
    vec2 center = vec2(0.5, 0.5);
    
    // Vector from center to current fragment
    vec2 dir = uv - center;
    
    // Correct for Y-axis direction (flip Y)
    dir.y = -dir.y;
    
    // Calculate distance from center
    float dist = length(dir);
    
    // Define radius
    float radius = 0.4;
    
    // Calculate the edge factor for anti-aliasing, only for alpha
    float edge_alpha = smoothstep(radius - thickness - antialiasing, radius - thickness + antialiasing, dist) -
                       smoothstep(radius - antialiasing, radius + antialiasing, dist);
    
    // Calculate angle in degrees, ensuring range [0, 360)
    float angle = degrees(atan(dir.y, dir.x));
    if (angle < 0.0) {
        angle += 360.0;
    }
    
    // Calculate the end angle and progress angle, wrapping correctly
    float progress_angle = mod(start_angle - (progress * arc_span), 360.0);
    float end_angle = mod(start_angle - arc_span, 360.0);
    
    // Determine if the fragment should be part of the arc
    vec4 color = vec4(0.0);

    // Handle cases where the start_angle is greater or less than the end_angle
    bool within_arc = false;

    if (start_angle >= end_angle) {
        // Standard case: arc does not wrap around 0 degrees
        within_arc = (angle <= start_angle && angle >= end_angle);
    } else {
        // Wrapping case: arc spans across 0 degrees
        within_arc = (angle >= end_angle || angle <= start_angle);
    }

    if (within_arc) {
        if ((start_angle >= progress_angle && angle <= start_angle && angle >= progress_angle) ||
            (start_angle < progress_angle && (angle >= progress_angle || angle <= start_angle))) {
            color = color_fg;  // Inside the progress arc
        } else {
            color = vec4(color_bg.rgb*(1.0-uv.y*0.5), 1.0);  // Inside the arc but beyond progress
        }
    }

    // Apply anti-aliasing only to the alpha value
    COLOR = vec4(color.rgb, color.a * edge_alpha);

    // Calculate the pointer direction and position
    vec2 pointer_dir = vec2(cos(radians(progress_angle)), sin(radians(progress_angle)));

    // Determine if this fragment is along the pointer direction
    float pointer_dist = dot(dir, pointer_dir);
    float perpendicular_dist = length(dir - pointer_dir * pointer_dist);
    
    if (pointer_dist > 0.0 && perpendicular_dist < pointer_thickness && dist <= radius) {
        float pointer_edge_alpha = smoothstep(pointer_thickness + antialiasing, pointer_thickness - antialiasing, perpendicular_dist);
        vec4 pointer_color = vec4(color_pointer.rgb, color_pointer.a * pointer_edge_alpha);
        
        // Ensure the pointer is drawn above everything else
        COLOR = mix(COLOR, pointer_color, pointer_edge_alpha);
    }
}
