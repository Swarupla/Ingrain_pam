import { Component, ElementRef, OnInit, ViewChild, Input, OnChanges, AfterViewInit, Output, EventEmitter } from '@angular/core';
import * as d3 from 'd3';

@Component({
  selector: 'app-bubble-chart',
  templateUrl: './bubble-chart.component.html',
  styleUrls: ['./bubble-chart.component.scss']
})
export class BubbleChartComponent implements OnInit, OnChanges, AfterViewInit {

  @ViewChild('containerbubbleChart', { static: true }) element: ElementRef;
  @Input() data;
  @Output() bubbleNodeSelected  = new EventEmitter();
  constructor() { }
  mockSet = {
    'children': [{ 'Name': 'Olives', 'Count': 4319 },
    { 'Name': 'Tea', 'Count': 4159 },
    { 'Name': 'Mashed Potatoes', 'Count': 2583 },
    { 'Name': 'Boiled Potatoes', 'Count': 2074 },
    { 'Name': 'Milk', 'Count': 1894 },
    { 'Name': 'Chicken Salad', 'Count': 1809 },
    { 'Name': 'Vanilla Ice Cream', 'Count': 1713 },
    { 'Name': 'Cocoa', 'Count': 1636 },
    { 'Name': 'Lettuce Salad', 'Count': 1566 },
    { 'Name': 'Lobster Salad', 'Count': 1511 },
    { 'Name': 'Chocolate', 'Count': 1489 }]
  };

  htmlElement: HTMLElement;
  host;
  diameter = 300;

  ngOnInit() {
  }

  ngAfterViewInit() {
    this.htmlElement = this.element.nativeElement;
    this.host = d3.select(this.htmlElement);
    this.drawBubleChart();
  }

ngOnChanges() {
    this.ngAfterViewInit();
  }


  drawBubleChart() {
    const color = d3.scaleOrdinal(d3.schemePaired);

    if (!this.data) {
     this.data = this.mockSet;
    }

    const bubble = d3.pack(this.data)
      .size([this.diameter +  200, this.diameter + 100])
      .padding(20);

    this.host.html('');

    const svg = this.host
      .append('svg')
      .attr('width', this.diameter + 200)
      .attr('height', this.diameter + 100)
      .attr('class', 'bubble');

    const nodes = d3.hierarchy(this.data)
      .sum(function (d) { return d.Count; });
      // .sort(function(a, b) { return b.height - a.height || b.value - a.value; });
      // .sort(function (a, b) { return a.Count - b.Count; });
    const _this = this;
    const node = svg.selectAll('.node')
      .data(bubble(nodes).descendants())
      .enter()
      .filter(function (d) {
        return !d.children;
      })
      .append('g')
      .attr('class', 'node')
      .attr('transform', function (d) {
        return 'translate(' + (d.x + 50) + ',' + (d.y) + ')';
      })
      .on('click', function (item) {
        _this.bubbleNodeSelected.emit(item.data);
      });

    node.append('title')
      .text(function (d) {
        return d.data.Name + ': ' + d.data.Count;
      });

    node.append('circle')
      .attr('r', function (d , i) {
        // if ( d.r < 20) {
        //   return d.r + 15;
        // } else {
        return (d.r + 5);
        // }
        // return (i * 25);
      })
      .style('fill', function (d, i) {
        return color(i);
      })
      .style('cursor', 'pointer');

    node.append('text')
      .attr('dy', '.2em')
      .style('text-anchor', 'middle')
      .text(function (d) {
        return d.data.Name.substring(0, d.r / 3);
      })
      .attr('font-family', 'sans-serif')
      .attr('font-size', function (d) {
        return d.r / 7;
      })
      .attr('fill', 'white');

    node.append('text')
      .attr('dy', '1.3em')
      .style('text-anchor', 'middle')
      .text(function (d) {
        return d.data.Count;
      })
      .attr('font-family', 'Gill Sans', 'Gill Sans MT')
      .attr('font-size', function (d) {
        return d.r / 5;
      })
      .attr('fill', 'white');

    d3.select(self.frameElement)
      .style('height', this.diameter + 100 + 'px');

      this.element.nativeElement.scrollBy( this.diameter + 150 , 0);
  }
}
