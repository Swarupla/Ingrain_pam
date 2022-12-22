import { Component, OnInit, Input, ElementRef, ViewChild, AfterViewInit, Output, EventEmitter, OnChanges } from '@angular/core';
import * as d3 from 'd3';

@Component({
  selector: 'app-half-donut',
  templateUrl: './half-donut.component.html',
  styleUrls: ['./half-donut.component.scss']
})
export class HalfDonutComponent implements OnInit, AfterViewInit, OnChanges {

  htmlElement: any;
  host: any;
  fontCoordinates: any;
  scale = 1;


  constructor() { }

  @Input() donutType: any;
  @Input() hdvalue = 0;
  @Input() hdwidth;
  @Input() hdheight;
  @Input('tickOffset') offset?= 2;
  @Input() tickSize?= 7;
  @Input() fontSize?= 7;
  @Input() textOffset?= 2;
  @Input() thickness?= 30;
  @Input() colors?= ['#5EBBF8', '#008000']
  @Input() noOfTicks?= 10;
  @ViewChild('con', { static: true }) con: ElementRef;
  @Output() onclick = new EventEmitter();


  ngOnInit() {

  }

  ngOnChanges() {
    // this.noOfTicks = this.roundUp(this.hdvalue);
    this.scale = this.setScale(this.roundUp(this.hdvalue));
    this.ngAfterViewInit();
  }

  setScale(number) {
    return +(number / this.noOfTicks);
  }
  roundUp(number) {
    number = number ? number : 0;
    number = number / 10;
    const rounded = Math.ceil(number);
    return rounded * 10;
  }

  ngAfterViewInit() {

    this.htmlElement = this.con.nativeElement;
    this.host = d3.select(this.htmlElement);

    // Data
    let value;
    this.hdvalue ? value = this.hdvalue : value = 0;
    const text = value;
    const data = [value / (this.noOfTicks * this.scale), 1 - (value / (this.noOfTicks * this.scale))];

    // Settings
    const width = this.hdwidth - 3 * (this.offset + this.tickSize + this.fontSize + this.textOffset);
    const height = this.hdheight - 3 * (this.offset + this.tickSize + this.fontSize + this.textOffset);
    const anglesRange = 0.5 * Math.PI;
    const radis = Math.min(width, height) / 2;
    const thickness = this.thickness;

    // Utility
    const _this = this;
    const pies = d3.pie()
      .value(d => +d)
      .sort(null)
      .startAngle(anglesRange * -1)
      .endAngle(anglesRange);

    const arc = d3.arc()
      .outerRadius(radis)
      .innerRadius(radis - thickness);

    const translation = (x, y) => `translate(${x}, ${y})`;

    this.host.html('');

    const pointsGenerator = (centerPoints: {}, noOfTicks: number) => {
      const angleWidth = (Math.PI / this.noOfTicks);
      const tickpoints = [];
      this.fontCoordinates = [];
      for (let index = 0; index <= this.noOfTicks; index++) {

        const middle = Math.floor(this.noOfTicks / 2);
        let x = 0;
        let y = 0;

        x = width - (radis * Math.cos(index * angleWidth));
        y = height - (radis * Math.sin(index * angleWidth));

        const tickSize = this.tickSize;
        const offset = this.offset;
        let yIncrease = tickSize * Math.abs(Math.sin(index * angleWidth));
        let xIncrease = tickSize * Math.abs(Math.cos(index * angleWidth));
        let xoffset = offset * Math.abs(Math.cos(index * angleWidth));
        let yoffset = offset * Math.abs(Math.sin(index * angleWidth));

        let textdx = this.textOffset * Math.abs(Math.sin(index * angleWidth)) + xoffset + xIncrease;
        let textdy = this.textOffset * Math.abs(Math.cos(index * angleWidth)) + yoffset + yIncrease;

        if (index < middle) {
          tickpoints.push([x - xIncrease, y - yIncrease]);
          tickpoints.push([x - xoffset, y - yoffset]);
          this.fontCoordinates.push([x - xoffset - xIncrease, y, -textdx, -textdy]);
        } else if (index > middle) {
          tickpoints.push([x + xoffset, y - yoffset]);
          tickpoints.push([x + xIncrease, y - yIncrease]);
          this.fontCoordinates.push([x, y, textdx, -textdy]);
        } else if (index === middle) {
          tickpoints.push([x, y - yoffset]);
          tickpoints.push([x, y - tickSize]);
          this.fontCoordinates.push([x, y, 0, -textdy]);
        }
        tickpoints.push(null);
      }
      return tickpoints.reverse();
    };


    const centerPoints = { x: width / 2, y: height / 2 };

    const ticks = pointsGenerator(centerPoints, this.noOfTicks);


    const pathData = d3.line().defined(function (d) {
      return d !== null;
    })(ticks);

    const toolTipDiv = this.host.append('div')
      .attr('class', 'tooltip')
      .style('opacity', 0);

    const svg1 = this.host.append('svg')
      .attr('width', this.hdwidth)
      .attr('height', this.hdheight)
      .attr('viewbox', '0 0 100% 100%')
      .attr('preserveAspectRatio', 'xMidYMid meet')
      .attr('class', 'half-donut');

    const svg = svg1.append('g')
      .attr('transform', translation(this.hdwidth / 2, this.hdheight / 2));

    svg.selectAll('path')
      .data(pies(data))
      .enter()
      .append('path')
      .attr('fill', (d, i) => this.colors[i])
      .attr('d', arc);

    svg.append('path').attr('fill', 'black')
      .attr('d', pathData)
      .attr('stroke', 'black')
      .attr('transform', `translate(${-width}, ${-height})`)
      .attr('stroke-width', '3');

    var _scale = this.scale;
    svg.append('text').text(d => text)
      .attr('dy', '13')
      .attr('class', 'label')
      .attr('text-anchor', 'middle');

    svg.selectAll('.text').data(this.fontCoordinates).enter()
      .append('text').text((d, i) => {
        const scaleval = i * this.scale;
        return '' + scaleval;
      })
      .attr('x', d => d[0])
      .attr('y', d => d[1])
      .attr('dx', d => d[2])
      .attr('dy', d => d[3])
      .attr('transform', `translate(${-width}, ${-height})`)
      .attr('textLength', function (d, i) {
        if (i * _scale < 10) {
          return 7;
        } else if (i * _scale < 1000) {
          return 15;
        } else {
          return 20;
        }
      })
      .attr('lengthAdjust', 'spacingAndGlyphs')
      .style('font-family', 'auto')
      .style('font-size', 'smaller')
      .on('mouseover', function (d, i) {
        toolTipDiv.transition()
          .duration(200)
          .style('opacity', .9);
        toolTipDiv.html('<span style="color: black;font-weight:bold">'
          + '' + i * _scale + '</span>'
        ).style('position', 'absolute');
      })
      .on('mouseout', function (d) {
        toolTipDiv.transition()
          .duration(500)
          .style('opacity', 0)
          .style('position', 'absolute');
        toolTipDiv.html('');
      });
  }
  setLeft(event, donutType) {
    let left;
    if (donutType === 'mse') {
      left = (event.pageX) - 230 + 'px';
    } else {
      left = (event.pageX) - 50 + 'px';
    }
    return left;
  }
  setTop(event) {
    let top;
    top = (event.pageY - 28) - 550 + 'px';
    return top;
  }
  onClick() {
    this.onclick.emit();
  }
}
