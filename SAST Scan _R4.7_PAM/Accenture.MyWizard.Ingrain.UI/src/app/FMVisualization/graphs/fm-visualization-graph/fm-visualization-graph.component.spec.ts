import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { FmVisualizationGraphComponent } from './fm-visualization-graph.component';

describe('FmVisualizationGraphComponent', () => {
  let component: FmVisualizationGraphComponent;
  let fixture: ComponentFixture<FmVisualizationGraphComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ FmVisualizationGraphComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(FmVisualizationGraphComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
