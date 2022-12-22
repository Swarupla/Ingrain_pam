import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TargetNodeComponent } from './target-node.component';

describe('TargetNodeComponent', () => {
  let component: TargetNodeComponent;
  let fixture: ComponentFixture<TargetNodeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ TargetNodeComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(TargetNodeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
