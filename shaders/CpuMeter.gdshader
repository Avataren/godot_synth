shader_type canvas_item;

uniform float cpu_usage : hint_range(0.0, 1.0); // CPU usage, between 0 and 1
uniform vec4 frame_color = vec4(1.0, 1.0, 1.0, 1.0); // Frame color
uniform float frame_thickness = 0.05; // Thickness of the frame as a fraction of the smallest dimension

void fragment() {
    const int num_dots = 100; // Define the number of dots
    float dot_width = 1.0 / float(num_dots); // Calculate the width of each dot based on UV coordinates
    int current_dot = int(UV.x / dot_width); // Determine which dot is being rendered
    int visible_dots = int(cpu_usage * float(num_dots)); // Calculate the threshold for visible dots based on CPU usage

    // Adjusting the green color brightness down by about 25%
    vec4 start_color = vec4(0.0, 0.75, 0.0, 1.0); // Green
    vec4 mid_color = vec4(1.0, 1.0, 0.0, 1.0); // Yellow
    vec4 end_color = vec4(1.0, 0.0, 0.0, 1.0); // Red

    // Calculate color based on the position in the gradient
    vec4 color;
    if (current_dot < num_dots / 2) {
        float t = float(current_dot) / float(num_dots / 2);
        color = mix(start_color, mid_color, t);
    } else {
        float t = float(current_dot - num_dots / 2) / float(num_dots / 2);
        color = mix(mid_color, end_color, t);
    }

    // Determine if within frame
    float frame_thickness_adjusted = frame_thickness * min(SCREEN_PIXEL_SIZE.x, SCREEN_PIXEL_SIZE.y);
    bool in_frame = (UV.x < frame_thickness_adjusted || UV.x > 1.0 - frame_thickness_adjusted || UV.y < frame_thickness_adjusted || UV.y > 1.0 - frame_thickness_adjusted);

    if (in_frame) {
        COLOR = frame_color; // Frame color
    } else if (current_dot < visible_dots) {
        COLOR = vec4(color.rgb, 1.0); // Fully opaque for visible dots
    } else {
        COLOR = vec4(0.2, 0.3, 0.4, 1.0); // Background color where no dots are visible
    }
}
