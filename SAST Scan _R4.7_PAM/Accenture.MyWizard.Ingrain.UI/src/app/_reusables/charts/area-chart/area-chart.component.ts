import { Component, OnInit, AfterViewInit, Input, OnChanges } from '@angular/core';
import * as d3 from 'd3';

@Component({
  selector: 'app-area-chart',
  templateUrl: './area-chart.component.html',
  styleUrls: ['./area-chart.component.scss']
})
export class AreaChartComponent implements OnInit, AfterViewInit, OnChanges {

  @Input() data: number;
  @Input() memoryUsageData;
  @Input() componentName: string;

  constructor() { }

  htmlElement: HTMLElement;

  ngOnInit() { }

  ngOnChanges() {
    d3.select('#' + this.componentName).selectAll('*').remove();
    this.ngAfterViewInit();
  }

  ngAfterViewInit() {
    const t = d3.select('#' + this.componentName);
    if (this.data === undefined || this.data === null || this.data === 0) { } else {
      const data = [4, 2, 6, 3, 3, 7, 9, 2, 1, 6];

      data.push(+this.data);
      const width = 210, height = 63;

      const x = d3.scaleLinear()
        .range([0, width])
        .domain([0, data.length - 1]);

      const y = d3.scaleLinear()
        .range([height, 0])
        .domain([d3.min(data), d3.max(data)]);

      const line = d3.area()
        .x(function (d, i) { return x(i); })
        .y1(function (d) { return y(d); })
        .y0(height)
        .curve(d3.curveCardinal);

      const svg = t.append('svg')
        .attr('width', '100%')
        .attr('height', height)
        .append('g')
        .attr('transform', 'translate(0, 0)');
      svg.append('g')
        .attr('class', 'x axis')
        .attr('transform', 'translate(0,' + height + ')');

      svg.append('g')
        .attr('class', 'y axis');

      svg.append('path')
        .datum(data)
        .attr('class', 'line')
        .attr('d', line)
        .attr('fill', '#cfe7d8');
    }
  }
}
