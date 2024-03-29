# OpenStreetMap Tiles

A tile renderer for OpenStreetMap that provides a dynamic overlay tile set.

- [Road overlay](#road-overlay)
- [Rail overlay](#rail-overlay)
- [All overlay](#all-overlay)
- [Limitations and notes](#limitations-and-notes)
- [To do](#to-do)

A live version is running at https://osm-tiles.james-ross.co.uk/.

A test page for any OpenStreetMap tags is at https://osm-tiles.james-ross.co.uk/test.html.

[![Build status](https://ci.appveyor.com/api/projects/status/n7l46b5cjdrxhtmg?svg=true)](https://ci.appveyor.com/project/twpol/osm-tiles)

## Road overlay

This tile set is an overlay (meaning the tiles are transparent where there is no data) which renders roads (highways) from OpenStreetMap according to their various tags, including:

- `cycleway=lane =opposite_lane`
- `cycleway:both=lane =opposite_lane`
- `cycleway:left=lane =opposite_lane`
- `cycleway:left:oneway=no`
- `cycleway:right=lane =opposite_lane`
- `cycleway:right:oneway=no`
- `highway=motorway =trunk =primary =secondary =tertiary =unclassified =residential =service =motorway_link =trunk_link =primary_link =secondary_link =tertiary_link`
- `lanes=*`
- `lanes:backward=*`
- `lanes:both_ways=*`
- `lanes:forward=*`
- `layer=*`
- `oneway=yes =-1`
- `parking:lane:both=parallel =diagonal =perpendicular`
- `parking:lane:left=parallel =diagonal =perpendicular`
- `parking:lane:right=parallel =diagonal =perpendicular`
- `shoulder=yes =both =left =right`
- `shoulder:both=yes`
- `shoulder:left=yes`
- `shoulder:right=yes`
- `sidewalk=both =left =right`
- `verge=yes =both =left =right`
- `verge:both=yes`
- `verge:left=yes`
- `verge:right=yes`

![Example road tile](Documentation/roads-18-131004-87172.png)

## Rail overlay

This tile set is an overlay (meaning the tiles are transparent where there is no data) which renders rails (railways) from OpenStreetMap according to their various tags, including:

- `gauge=*`
- `railway=abandoned =construction =disused =funicular =light_rail =monorail =narrow_gauge =preserved =rail =subway =tram`

![Example rail tile](Documentation/rails-18-131004-87172.png)

## All overlay

![Example all tile](Documentation/all-18-131004-87172.png)

## Limitations and notes

- Only zoom levels 16 through 22 are supported
- Each tile is rendered on-demand, but the underlying data is cached in memory in zoom level 14 chunks

## To do

- Separation and correction of way end nodes at junctions (to align lanes)
- Calculations for bus, PSV (public service vehicle), other specialised lanes
- Calculations for `placement=*`
- Calculations using turn lane markings
- Calculations for `width`
- Display tapering of road width/lanes
- Display parking lanes/bays
- Display bridges, tunnels
- Display bus, PSV, other specialised lanes
- Display turn lane markings (left/right turn arrows, etc.)
- Display of footpaths
- Display of railway platforms
