import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CurvedHorizontalBarGraphComponent } from './curved-horizontal-bar-graph.component';

describe('CurvedHorizontalBarGraphComponent', () => {
  let component: CurvedHorizontalBarGraphComponent;
  let fixture: ComponentFixture<CurvedHorizontalBarGraphComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CurvedHorizontalBarGraphComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CurvedHorizontalBarGraphComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
