# OpenStreetMap Tiles

A tile renderer for OpenStreetMap that provides the following tile sets:

- Road overlay

## Road overlay

This tile set is an overlay (meaning the tiles are transparent where there is no data) which renders roads (highways) from OpenStreetMap according to their various tags, including:

- `cycleway:left=lane`
- `cycleway:right=lane`
- `cycleway=lane =opposite`
- `highway=motorway =trunk =primary =secondary =tertiary =unclassified =residential =service =motorway_link =trunk_link =primary_link =secondary_link =tertiary_link`
- `lanes=*`
- `oneway=yes`
- `parking:lanes:both=parallel =diagonal =perpendicular`
- `parking:lanes:left=parallel =diagonal =perpendicular`
- `parking:lanes:right=parallel =diagonal =perpendicular`

### Limitations

- Only `layer=0` or `!layer` ways are rendered for now.
- Only zoom level 18 is supported for now.

## Notes

- Each tile is rendered on-demand, nothing is cached.
- Each tile will make an Overpass API query to fetch the OpenStreetMap data needed.

## Live example

A live tile server is running at https://osm-tiles.james-ross.co.uk/. To use the tile sets, add the following TMS URLs to your map:

- https://osm-tiles.james-ross.co.uk/overlays/roads/{zoom}/{x}/{y}.png
