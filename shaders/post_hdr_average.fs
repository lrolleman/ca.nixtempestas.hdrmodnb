#version 140

// post_hdr_average.fs

#ifdef GL_ES
precision mediump float;
#endif

uniform sampler2D PostSourceTexture;
uniform sampler2D HdrAvgLuminance;
uniform vec4 PostBrightParams;

in vec2 v_TexCoord;
in vec4 v_Color;

out vec4 out_FragColor;

void main()
{
    // Read the original HDR value
    float average = texture(PostSourceTexture, vec2(0.5, 0.5)).x;

    // Here we do the same calculations we were doing on the CPU; this saves us doing an expensive (and
    // annoying) download to the CPU. We don't need to, since this is the EXACT same logic used in the 
    // CPU-side code. Sure, we're doing a full-screen pass on a 1x1 pixel framebuffer, but I think that'll be
    // ok...
    float running_avg = texture(HdrAvgLuminance, vec2(0.5, 0.5)).x;

    // We still occasionally get a NaN or inf coming through the pipeline. This will vary by card, precision, etc,
    // but if we get them, it nukes our average and the scene becomes permanently bloomed out. 
    // Rather than suffer the indignity of that, let's clamp invalid luminance values to something
    // sensible, and keep our scene looking nice.
    if (isnan(average) || isinf(average))
        average = running_avg;

    const float lumAdjustSpeedDark =   1.0;
    const float lumAdjustSpeedBright = 1.0;

    float total_delta = average - running_avg;
    float avgLumDelta;
    if (total_delta < 0)
        avgLumDelta = max(PostBrightParams.x * lumAdjustSpeedDark * total_delta, total_delta);
    else
        avgLumDelta = min(PostBrightParams.x * lumAdjustSpeedBright * total_delta, total_delta);

    // The CPU side code is going to ping-pong the HdrAvgLuminance buffer between two. So the next time 
    // this shader is run, this output will be the input. 
    out_FragColor = vec4(avgLumDelta + running_avg, 0.0, 0.0, 0.0);
}
